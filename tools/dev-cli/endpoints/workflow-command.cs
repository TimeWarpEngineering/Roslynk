#region Purpose
// Mode-aware CI/CD pipeline for Roslynk (what .github/workflows/workflow.yml runs)
#endregion
#region Design
// PR/merge: build -> test -> pack. Pack writes the IsPackable set (TimeWarp.Roslynk) to
// artifacts/packages and verifies the file names with CiRunPromotion; the workflow uploads it
// as Packages-<run number>.
// Release: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push.
// Release promotes the CI-built Packages-* artifact (build once, promote) and never rebuilds.
// The push target is the org GitHub Packages feed only; nuget.org is refused by a hard guard,
// because Roslynk on nuget.org is Peter Morris's upstream package.
// DevCli (TimeWarp.Nuru.DevCli 4.0.0-beta.11) ships check-version and release but no workflow
// command, so this is a local copy of timewarp-taratibu's, which hard-codes the same GitHub
// Packages target (task 008). --mode and --api-key match workflow.yml (api-key = GITHUB_TOKEN).
// workflow_dispatch without --mode stays merge and does not publish. Handlers run in-process,
// so CI needs no ./bin/dev. `dev release` remains the human tag + GitHub Release cut.
#endregion

using System.Globalization;

namespace DevCli.Endpoints;

[NuruRoute("workflow", Description = "Execute full CI/CD pipeline (mode-aware)")]
public sealed class WorkflowCommand : ICommand<Unit>
{
  /// <summary>
  /// Org GitHub Packages NuGet feed. Private repo packages must never go to NuGet.org.
  /// </summary>
  internal const string GitHubPackagesSource = "https://nuget.pkg.github.com/TimeWarpEngineering/index.json";

  [Option("mode", "m", Description = "CI mode: pr, merge, or release (auto-detected from GITHUB_EVENT_NAME)")]
  public string? Mode { get; set; }

  [Option("api-key", Description = "NuGet API key for GitHub Packages (GITHUB_TOKEN in Actions)")]
  public string? ApiKey { get; set; }

  internal sealed class Handler : ICommandHandler<WorkflowCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly IRepoConfigService RepoConfigService;
    private readonly IPackableProjectService PackableProjectService;
    private CancellationToken Ct;
    private string? ApiKey;

    public Handler
    (
      ITerminal terminal,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService repoConfigService,
      IPackableProjectService packableProjectService
    )
    {
      Terminal = terminal;
      NuGetVersionService = nuGetVersionService;
      RepoConfigService = repoConfigService;
      PackableProjectService = packableProjectService;
    }

    public async Task<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      Ct = ct;
      ApiKey = command.ApiKey;

      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      CiMode mode;
      try
      {
        mode = CiModeDetector.DetermineMode(command.Mode, eventName);
      }
      catch (ArgumentException exception)
      {
        Terminal.WriteErrorLine(exception.Message);
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      if (string.IsNullOrEmpty(command.Mode))
      {
        string displayEventName = eventName ?? "(not set)";
        Terminal.WriteLine($"Detected GITHUB_EVENT_NAME: {displayEventName} -> Mode: {mode}");
      }

      Terminal.WriteLine($"\nCI/CD Pipeline — Mode: {mode}\n");

      if (mode == CiMode.Release)
      {
        await RunReleaseAsync();
      }
      else
      {
        await RunPrAsync();
      }

      return Unit.Value;
    }

    private async Task RunPrAsync()
    {
      Terminal.WriteLine("Pipeline: build → test → pack\n");
      Environment.ExitCode = 0;

      if (!await RunStepAsync("Build", new BuildCommand.Handler(Terminal).Handle(new BuildCommand(), Ct)))
      {
        return;
      }

      if (!await RunStepAsync("Test", new TestCommand.Handler(Terminal).Handle(new TestCommand(), Ct)))
      {
        return;
      }

      if (!await RunStepAsync("Pack", new PackCommand.Handler(Terminal, PackableProjectService).Handle(new PackCommand(), Ct)))
      {
        return;
      }

      Terminal.WriteLine("\nPipeline SUCCEEDED".Green());
    }

    private async Task RunReleaseAsync()
    {
      Terminal.WriteLine("Pipeline: tag-gate → check-version → locate-run → download-artifact → verify → push\n");
      Environment.ExitCode = 0;

      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        AbortPipeline("repository root not found");
        return;
      }

      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 1/6: Release Gate — Tag Assertions");
      Terminal.WriteLine("===============================================================================");

      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      string? propsVersion = ReadPropsVersion(repoRoot);

      if (eventName == "release")
      {
        string? refName = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
        TagAssertionResult tagResult = TagAssertion.Validate(refName, propsVersion);

        if (!tagResult.IsValid)
        {
          Terminal.WriteErrorLine($"Release gate failed: {tagResult.Error}");
          AbortPipeline("release tag does not match source version");
          return;
        }

        Terminal.WriteLine($"Tag assertion passed: {tagResult.ExpectedTag}");
      }
      else
      {
        Terminal.WriteLine("Tag assertion skipped: GITHUB_EVENT_NAME is not 'release' (break-glass/local release has no triggering ref tag to assert against; the tag-pin check below still applies).");
      }

      if (string.IsNullOrWhiteSpace(propsVersion))
      {
        Terminal.WriteLine("Tag pin skipped: could not read <Version> from source/Directory.Build.props (Step 2/6 Check Version will fail with details).");
      }
      else
      {
        TagPinOutcome tagPinOutcome = await CheckTagPinAsync(propsVersion);
        string pinTag = $"v{propsVersion}";

        switch (tagPinOutcome.Status)
        {
          case TagPinStatus.NoTag:
            Terminal.WriteLine($"Tag pin: {pinTag} not yet tagged.");
            break;

          case TagPinStatus.Match:
            Terminal.WriteLine($"Tag pin passed: HEAD is at {pinTag}.");
            break;

          case TagPinStatus.Mismatch:
            Terminal.WriteErrorLine($"Release gate failed: tag {pinTag} already exists at commit {ShortSha(tagPinOutcome.TagCommit)}; this run is at {ShortSha(tagPinOutcome.HeadCommit)}. A partial-publish resume must run from the tag's commit (or bump the version if source changed).");
            AbortPipeline("tag pin mismatch");
            return;

          case TagPinStatus.GitError:
            Terminal.WriteErrorLine($"Release gate failed: tag pin check could not run — {tagPinOutcome.Detail}");
            AbortPipeline("tag pin check could not run");
            return;

          default:
            Terminal.WriteErrorLine($"Release gate failed: unhandled tag pin status '{tagPinOutcome.Status}'.");
            AbortPipeline("unhandled tag pin status");
            return;
        }
      }

      AncestorCheckOutcome ancestorOutcome = await CheckHeadAncestorOfMasterAsync();

      switch (ancestorOutcome.Status)
      {
        case AncestorCheckStatus.NotAncestor:
          Terminal.WriteErrorLine("Release gate failed: current commit is not an ancestor of master. Releases must be cut from commits on master.");
          AbortPipeline("commit not on master");
          return;

        case AncestorCheckStatus.MasterUnresolvable:
          Terminal.WriteErrorLine("Release gate failed: cannot resolve origin/master or master — ensure the checkout has full history (fetch-depth: 0) and a master ref exists.");
          AbortPipeline("master ref unresolvable");
          return;

        case AncestorCheckStatus.GitError:
          Terminal.WriteErrorLine($"Release gate failed: ancestor check could not run — {ancestorOutcome.Detail}");
          AbortPipeline("ancestor check could not run");
          return;

        case AncestorCheckStatus.Ancestor:
          break;

        default:
          Terminal.WriteErrorLine($"Release gate failed: unhandled ancestor check status '{ancestorOutcome.Status}'.");
          AbortPipeline("unhandled ancestor check status");
          return;
      }

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 2/6: Check Version");
      Terminal.WriteLine("===============================================================================");
      CheckVersionCommand.Handler checkVersionHandler = new(
        Terminal,
        NuGetVersionService,
        RepoConfigService,
        PackableProjectService);
      if (!await RunStepAsync("Check Version", checkVersionHandler.Handle(new CheckVersionCommand(), Ct)))
      {
        AbortPipeline("version already released");
        return;
      }

      IReadOnlyList<PackableProject> packableProjects = await PackableProjectService
        .GetPackableProjectsAsync(repoRoot, Ct);

      if (packableProjects.Count == 0)
      {
        Terminal.WriteErrorLine("Release gate failed: no packable projects found under source/.");
        AbortPipeline("no packable projects found");
        return;
      }

      Terminal.WriteLine($"Packable set ({packableProjects.Count}): {string.Join(", ", packableProjects.Select(project => project.PackageId))}");

      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 3/6: Locate CI Run");
      Terminal.WriteLine("===============================================================================");

      LocateRunOutcome locateOutcome = await LocateCiRunAsync();

      switch (locateOutcome.Status)
      {
        case LocateRunStatus.GhUnavailable:
          Terminal.WriteErrorLine("Release gate failed: release mode promotes CI-built artifacts and requires the gh CLI. On runners GH_TOKEN is provided by workflow.yml; locally install gh and run 'gh auth login'.");
          AbortPipeline("gh CLI unavailable");
          return;

        case LocateRunStatus.GhFailed:
          Terminal.WriteErrorLine($"Release gate failed: gh run list failed — {locateOutcome.Detail}. If this is transient (network/rate limit), retry; for auth issues run 'gh auth login'.");
          AbortPipeline("gh run list failed");
          return;

        case LocateRunStatus.NoMatchingRun:
          Terminal.WriteErrorLine($"Release gate failed: no successful CI run of workflow.yml exists for commit {locateOutcome.HeadSha}. Only tested CI artifacts are published — this commit must pass CI first. If a run failed, fix and re-run it (gh run rerun <run-id>).");
          AbortPipeline("no successful CI run found");
          return;

        case LocateRunStatus.Found:
          break;

        default:
          Terminal.WriteErrorLine($"Release gate failed: unhandled locate-run status '{locateOutcome.Status}'.");
          AbortPipeline("unhandled locate-run status");
          return;
      }

      Terminal.WriteLine($"Found {locateOutcome.CandidateRuns.Count} candidate CI run(s) for commit {ShortSha(locateOutcome.HeadSha)}.");

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 4/6: Download Artifact");
      Terminal.WriteLine("===============================================================================");

      DownloadArtifactOutcome downloadOutcome = await DownloadPackagesArtifactAsync(repoRoot, locateOutcome.CandidateRuns);

      if (downloadOutcome.Status == DownloadArtifactStatus.Exhausted)
      {
        if (downloadOutcome.ExpiredEncounters.Count > 0)
        {
          string expiredDetail = string.Join("; ", downloadOutcome.ExpiredEncounters.Select(encounter => $"run {encounter.RunId} ({encounter.Event}): {string.Join(", ", encounter.ArtifactNames)}"));
          Terminal.WriteErrorLine($"Release gate failed: every candidate CI run's Packages-* artifact has expired — {expiredDetail}. Re-run CI to produce a fresh tested artifact (gh run rerun {downloadOutcome.ExpiredEncounters[0].RunId}).");
        }
        else
        {
          Terminal.WriteErrorLine($"Release gate failed: no candidate CI run for commit {locateOutcome.HeadSha} uploaded a Packages-* artifact. Re-run CI to produce one (gh run rerun {locateOutcome.CandidateRuns[0].DatabaseId}).");
        }

        AbortPipeline("no usable CI artifact found");
        return;
      }

      Terminal.WriteLine($"Downloaded '{downloadOutcome.ArtifactName}' from run {downloadOutcome.Run!.DatabaseId} ({downloadOutcome.Run.Event}).");

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 5/6: Verify Package Set");
      Terminal.WriteLine("===============================================================================");

      if (string.IsNullOrWhiteSpace(propsVersion))
      {
        AbortPipeline("could not read version for package-set verification");
        return;
      }

      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      string[] actualNupkgPaths = Directory.Exists(artifactsDir) ? Directory.GetFiles(artifactsDir, "*.nupkg") : [];
      IReadOnlyList<string> actualFileNames = [.. actualNupkgPaths.Select(path => Path.GetFileName(path))];

      PackageSetVerification verification = CiRunPromotion.VerifyPackageSet(actualFileNames, packableProjects, propsVersion);

      if (!verification.IsMatch)
      {
        if (verification.Missing.Count > 0)
        {
          Terminal.WriteErrorLine($"Release gate failed: downloaded artifact is missing package(s): {string.Join(", ", verification.Missing)}.");
        }

        if (verification.Unexpected.Count > 0)
        {
          Terminal.WriteErrorLine($"Release gate failed: downloaded artifact has unexpected package(s): {string.Join(", ", verification.Unexpected)}.");
        }

        Terminal.WriteErrorLine($"CI run likely predates the version bump — re-run CI on commit {locateOutcome.HeadSha} and retry.");
        AbortPipeline("downloaded package set does not match derived packable set");
        return;
      }

      Terminal.WriteLine("Package set verified.");

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Step 6/6: Push to GitHub Packages");
      Terminal.WriteLine("===============================================================================");
      await PushPackagesAsync(repoRoot, packableProjects, ApiKey);

      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine("  Pipeline SUCCEEDED - Packages published to GitHub Packages");
      Terminal.WriteLine("===============================================================================");
    }

    private async Task<bool> RunStepAsync(string stepName, Task<Unit> step)
    {
      await step;

      if (Environment.ExitCode != 0)
      {
        Terminal.WriteErrorLine($"\nPipeline FAILED — {stepName} failed".Red());
        return false;
      }

      return true;
    }

    private void AbortPipeline(string reason)
    {
      Terminal.WriteLine("");
      Terminal.WriteLine("===============================================================================");
      Terminal.WriteLine($"  Pipeline ABORTED — {reason}");
      Terminal.WriteLine("===============================================================================");
      Environment.ExitCode = 1;
    }

    private static string? ReadPropsVersion(string repoRoot) => PropsVersionReader.Read(repoRoot);

    private static string ShortSha(string? sha) =>
      string.IsNullOrEmpty(sha) ? "(unknown)" : sha.Length > 7 ? sha[..7] : sha;

    private static async Task<TagPinOutcome> CheckTagPinAsync(string version)
    {
      string tag = $"v{version}";

      CommandOutput verifyResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "-q", "--verify", $"refs/tags/{tag}")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (verifyResult.ExitCode != 0)
      {
        if (!string.IsNullOrWhiteSpace(verifyResult.Stderr))
        {
          return new TagPinOutcome(TagPinStatus.GitError, null, null, verifyResult.Stderr.Trim());
        }

        return new TagPinOutcome(TagPinStatus.NoTag, null, null, null);
      }

      CommandOutput tagCommitResult = await Shell.Builder("git")
        .WithArguments("rev-parse", $"{tag}^{{commit}}")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (tagCommitResult.ExitCode != 0)
      {
        return new TagPinOutcome(TagPinStatus.GitError, null, null, tagCommitResult.Stderr.Trim());
      }

      CommandOutput headResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (headResult.ExitCode != 0)
      {
        return new TagPinOutcome(TagPinStatus.GitError, null, null, headResult.Stderr.Trim());
      }

      string tagCommit = tagCommitResult.Stdout.Trim();
      string headCommit = headResult.Stdout.Trim();

      return string.Equals(tagCommit, headCommit, StringComparison.Ordinal)
        ? new TagPinOutcome(TagPinStatus.Match, tagCommit, headCommit, null)
        : new TagPinOutcome(TagPinStatus.Mismatch, tagCommit, headCommit, null);
    }

    private async Task<AncestorCheckOutcome> CheckHeadAncestorOfMasterAsync()
    {
      string masterRef = "origin/master";

      CommandOutput verifyResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "--verify", "origin/master")
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (verifyResult.ExitCode != 0)
      {
        masterRef = "master";

        CommandOutput fallbackVerifyResult = await Shell.Builder("git")
          .WithArguments("rev-parse", "--verify", "master")
          .WithNoValidation()
          .CaptureAsync(CancellationToken.None);

        if (fallbackVerifyResult.ExitCode != 0)
        {
          return new AncestorCheckOutcome(AncestorCheckStatus.MasterUnresolvable, null);
        }

        Terminal.WriteLine("origin/master not found; using local master.");
      }

      CommandOutput ancestorResult = await Shell.Builder("git")
        .WithArguments("merge-base", "--is-ancestor", "HEAD", masterRef)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      if (ancestorResult.ExitCode == 0)
      {
        return new AncestorCheckOutcome(AncestorCheckStatus.Ancestor, null);
      }

      if (ancestorResult.ExitCode == 1)
      {
        return new AncestorCheckOutcome(AncestorCheckStatus.NotAncestor, null);
      }

      return new AncestorCheckOutcome(AncestorCheckStatus.GitError, ancestorResult.Stderr.Trim());
    }

    private async Task<LocateRunOutcome> LocateCiRunAsync()
    {
      CommandOutput headResult = await Shell.Builder("git")
        .WithArguments("rev-parse", "HEAD")
        .WithNoValidation()
        .CaptureAsync(Ct);

      if (headResult.ExitCode != 0)
      {
        throw new InvalidOperationException($"Could not determine HEAD commit: {headResult.Stderr.Trim()}");
      }

      string headSha = headResult.Stdout.Trim();

      CommandOutput runListResult;
      try
      {
        runListResult = await Shell.Builder("gh")
          .WithArguments("run", "list", "--workflow", "workflow.yml", "--commit", headSha, "--status", "success", "--json", "databaseId,event,headSha,createdAt")
          .WithNoValidation()
          .CaptureAsync(Ct);
      }
      catch (System.ComponentModel.Win32Exception)
      {
        return new LocateRunOutcome(LocateRunStatus.GhUnavailable, headSha, [], null);
      }

      if (runListResult.ExitCode != 0)
      {
        return new LocateRunOutcome(LocateRunStatus.GhFailed, headSha, [], runListResult.Stderr.Trim());
      }

      List<CiRunSummary>? runs = JsonSerializer.Deserialize(runListResult.Stdout, DevCliJsonContext.Default.ListCiRunSummary);
      IReadOnlyList<CiRunSummary> candidateRuns = CiRunPromotion.OrderCandidateRuns(runs ?? [], headSha);

      return candidateRuns.Count == 0
        ? new LocateRunOutcome(LocateRunStatus.NoMatchingRun, headSha, [], null)
        : new LocateRunOutcome(LocateRunStatus.Found, headSha, candidateRuns, null);
    }

    private async Task<DownloadArtifactOutcome> DownloadPackagesArtifactAsync(string repoRoot, IReadOnlyList<CiRunSummary> candidateRuns)
    {
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      List<ExpiredArtifactEncounter> expiredEncounters = [];

      foreach (CiRunSummary run in candidateRuns)
      {
        CommandOutput artifactsResult = await Shell.Builder("gh")
          .WithArguments("api", $"repos/{{owner}}/{{repo}}/actions/runs/{run.DatabaseId}/artifacts")
          .WithNoValidation()
          .CaptureAsync(Ct);

        if (artifactsResult.ExitCode != 0)
        {
          throw new InvalidOperationException($"Failed to list artifacts for run {run.DatabaseId}: {artifactsResult.Stderr.Trim()}");
        }

        RunArtifactListResponse? artifactList = JsonSerializer.Deserialize(artifactsResult.Stdout, DevCliJsonContext.Default.RunArtifactListResponse);
        PackagesArtifactOutcome selectOutcome = CiRunPromotion.SelectPackagesArtifact(artifactList?.Artifacts ?? []);

        if (selectOutcome.Status == PackagesArtifactStatus.Expired)
        {
          expiredEncounters.Add(new ExpiredArtifactEncounter(run.DatabaseId, run.Event, selectOutcome.ExpiredNames));
          continue;
        }

        if (selectOutcome.Status == PackagesArtifactStatus.NoneMatching)
        {
          continue;
        }

        RunArtifact artifact = selectOutcome.Artifact!;

        if (Directory.Exists(artifactsDir))
        {
          Directory.Delete(artifactsDir, recursive: true);
        }

        Directory.CreateDirectory(artifactsDir);

        Terminal.WriteLine($"Downloading artifact '{artifact.Name}' from run {run.DatabaseId} ({run.Event})...");

        int exitCode = await Shell.Builder("gh")
          .WithArguments("run", "download", run.DatabaseId.ToString(CultureInfo.InvariantCulture), "--name", artifact.Name, "--dir", artifactsDir)
          .WithWorkingDirectory(repoRoot)
          .WithNoValidation()
          .RunAsync(Ct);

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to download artifact '{artifact.Name}' from run {run.DatabaseId}!");
        }

        return new DownloadArtifactOutcome(DownloadArtifactStatus.Downloaded, run, artifact.Name, expiredEncounters);
      }

      return new DownloadArtifactOutcome(DownloadArtifactStatus.Exhausted, null, null, expiredEncounters);
    }

    private async Task PushPackagesAsync(string repoRoot, IReadOnlyList<PackableProject> projects, string? apiKey)
    {
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      string? version = ReadPropsVersion(repoRoot);

      if (string.IsNullOrEmpty(version))
      {
        throw new InvalidOperationException("Could not determine version for push");
      }

      HashSet<string> expectedNupkgFileNames = [.. projects.Select(project => $"{project.PackageId}.{version}.nupkg")];

      string[] actualNupkgFiles = Directory.Exists(artifactsDir)
        ? Directory.GetFiles(artifactsDir, $"*.{version}.nupkg")
        : [];

      List<string> unexpectedNupkgFileNames = [];
      foreach (string filePath in actualNupkgFiles)
      {
        string fileName = Path.GetFileName(filePath);
        if (!expectedNupkgFileNames.Contains(fileName))
        {
          unexpectedNupkgFileNames.Add(fileName);
        }
      }

      if (unexpectedNupkgFileNames.Count > 0)
      {
        throw new InvalidOperationException($"Unexpected package(s) in {artifactsDir} not in the derived packable set: {string.Join(", ", unexpectedNupkgFileNames)}");
      }

      string source = GitHubPackagesSource;
      AssertAllowedPublishSource(source);

      foreach (PackableProject project in projects)
      {
        string nupkgPath = Path.Combine(artifactsDir, $"{project.PackageId}.{version}.nupkg");

        if (!File.Exists(nupkgPath))
        {
          throw new FileNotFoundException($"Package not found: {nupkgPath}");
        }

        Terminal.WriteLine($"Pushing {project.PackageId}.{version}.nupkg to GitHub Packages...");

        List<string> args = ["nuget", "push", nupkgPath, "--source", source, "--skip-duplicate"];

        if (!string.IsNullOrEmpty(apiKey))
        {
          args.AddRange(["--api-key", apiKey]);
        }

        // Belt-and-suspenders: refuse if any arg still names nuget.org (e.g. a future edit).
        foreach (string arg in args)
        {
          AssertAllowedPublishSource(arg);
        }

        int exitCode = await Shell.Builder("dotnet")
          .WithArguments([.. args])
          .WithWorkingDirectory(repoRoot)
          .WithNoValidation()
          .RunAsync(Ct);

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Failed to push {project.PackageId}!");
        }
      }

      Terminal.WriteLine("\nAll packages pushed successfully!");
    }

    /// <summary>
    /// Hard guard: this private repo must never push to NuGet.org. Throws if
    /// <paramref name="sourceOrArg"/> looks like a nuget.org URL or host.
    /// </summary>
    internal static void AssertAllowedPublishSource(string sourceOrArg)
    {
      ArgumentNullException.ThrowIfNull(sourceOrArg);

      if (sourceOrArg.Contains("nuget.org", StringComparison.OrdinalIgnoreCase) ||
          sourceOrArg.Contains("api.nuget.org", StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException(
          "Refusing NuGet.org publish: TimeWarp.Roslynk is private (Roslynk on NuGet.org is Peter Morris's upstream package) and must only push to " +
          $"GitHub Packages ({GitHubPackagesSource}).");
      }
    }

    private enum AncestorCheckStatus
    {
      Ancestor,
      NotAncestor,
      MasterUnresolvable,
      GitError
    }

    private sealed record AncestorCheckOutcome(AncestorCheckStatus Status, string? Detail);

    private enum TagPinStatus
    {
      NoTag,
      Match,
      Mismatch,
      GitError
    }

    private sealed record TagPinOutcome(TagPinStatus Status, string? TagCommit, string? HeadCommit, string? Detail);

    private enum LocateRunStatus
    {
      Found,
      GhUnavailable,
      GhFailed,
      NoMatchingRun
    }

    private sealed record LocateRunOutcome(LocateRunStatus Status, string HeadSha, IReadOnlyList<CiRunSummary> CandidateRuns, string? Detail);

    private enum DownloadArtifactStatus
    {
      Downloaded,
      Exhausted
    }

    private sealed record DownloadArtifactOutcome(DownloadArtifactStatus Status, CiRunSummary? Run, string? ArtifactName, IReadOnlyList<ExpiredArtifactEncounter> ExpiredEncounters);

    private sealed record ExpiredArtifactEncounter(long RunId, string Event, IReadOnlyList<string> ArtifactNames);
  }
}
