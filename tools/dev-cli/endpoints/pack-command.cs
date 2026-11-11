#region Purpose
// Pack the derived IsPackable project set into artifacts/packages
#endregion
#region Design
// Discovers packable projects with DevCli's IPackableProjectService (MSBuild IsPackable under
// source/). Today that is one project, Morris.Roslynk.Cli (package TimeWarp.Roslynk):
// source/Directory.Build.props defaults IsPackable to false. Clears artifacts/packages first so
// leftovers cannot ship, reads <Version> from source/Directory.Build.props and verifies the
// nupkg set with CiRunPromotion. CI uploads that directory as Packages-*; release mode
// downloads it and never rebuilds. Shape copied from timewarp-taratibu.
#endregion

namespace DevCli.Endpoints;

[NuruRoute("pack", Description = "Pack packable projects under source/ into artifacts/packages")]
public sealed class PackCommand : ICommand<Unit>
{
  internal sealed class Handler : IRequestHandler<PackCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IPackableProjectService PackableProjectService;

    public Handler(ITerminal terminal, IPackableProjectService packableProjectService)
    {
      Terminal = terminal;
      PackableProjectService = packableProjectService;
    }

    public async ValueTask<Unit> Handle(PackCommand command, CancellationToken cancellationToken)
    {
      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      IReadOnlyList<PackableProject> packableProjects = await PackableProjectService
        .GetPackableProjectsAsync(repoRoot, cancellationToken);

      if (packableProjects.Count == 0)
      {
        Terminal.WriteErrorLine("Pack failed: no packable projects found under source/.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      string? version = PropsVersionReader.Read(repoRoot);
      if (string.IsNullOrWhiteSpace(version))
      {
        Terminal.WriteErrorLine("Pack failed: could not read <Version> from source/Directory.Build.props.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      if (Directory.Exists(artifactsDir))
      {
        Directory.Delete(artifactsDir, recursive: true);
      }

      Directory.CreateDirectory(artifactsDir);

      Terminal.WriteLine($"Packable set ({packableProjects.Count}): {string.Join(", ", packableProjects.Select(project => project.PackageId))}");
      Terminal.WriteLine($"Output: {artifactsDir}");

      foreach (PackableProject project in packableProjects)
      {
        Terminal.WriteLine($"Packing {project.PackageId} {version}...");
        int packExit = await Shell.Builder("dotnet")
          .WithArguments("pack", project.ProjectPath, "--configuration", "Release", "--output", artifactsDir)
          .WithWorkingDirectory(repoRoot)
          .WithNoValidation()
          .RunAsync(cancellationToken);

        if (packExit != 0)
        {
          Terminal.WriteErrorLine($"dotnet pack failed for {project.PackageId}.".Red());
          Environment.ExitCode = packExit;
          return Unit.Value;
        }
      }

      string[] actualNupkgPaths = Directory.GetFiles(artifactsDir, "*.nupkg");
      IReadOnlyList<string> actualFileNames = [.. actualNupkgPaths.Select(path => Path.GetFileName(path))];
      PackageSetVerification verification = CiRunPromotion.VerifyPackageSet(actualFileNames, packableProjects, version);

      if (!verification.IsMatch)
      {
        if (verification.Missing.Count > 0)
        {
          Terminal.WriteErrorLine($"Pack failed: missing package(s): {string.Join(", ", verification.Missing)}.");
        }

        if (verification.Unexpected.Count > 0)
        {
          Terminal.WriteErrorLine($"Pack failed: unexpected package(s): {string.Join(", ", verification.Unexpected)}.");
        }

        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine("Packed set verified.".Green());
      foreach (string fileName in actualFileNames.OrderBy(name => name, StringComparer.Ordinal))
      {
        Terminal.WriteLine($"  {fileName}");
      }

      return Unit.Value;
    }
  }
}
