#region Purpose
// Test command - runs every test project in source/Morris.Roslynk.slnx in Release
#endregion
#region Design
// Streams dotnet output and reports failure through Environment.ExitCode (never
// Environment.Exit), so the workflow command can run it as a pipeline step.
// Coverage is XPlat Code Coverage (coverlet.collector) under artifacts/coverage,
// which .github/workflows/workflow.yml uploads.
#endregion

namespace DevCli.Endpoints;

[NuruRoute("test", Description = "Run all tests in source/Morris.Roslynk.slnx in Release")]
public sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : IRequestHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TestCommand command, CancellationToken cancellationToken)
    {
      string? solutionPath = SolutionLocator.Find(Terminal);
      if (solutionPath is null)
      {
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine("Running Roslynk tests...");

      // solutionPath is source/Morris.Roslynk.slnx, so its parent is the repository root.
      string coverageDirectory = Path.Combine(
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionPath)!, "..")),
        "artifacts",
        "coverage");
      if (Directory.Exists(coverageDirectory))
      {
        Directory.Delete(coverageDirectory, recursive: true);
      }

      Directory.CreateDirectory(coverageDirectory);

      int exitCode = await Shell.Builder("dotnet")
        .WithArguments(
          "test",
          solutionPath,
          "--configuration",
          "Release",
          "--verbosity",
          "minimal",
          "--collect",
          "XPlat Code Coverage",
          "--results-directory",
          coverageDirectory)
        .WithNoValidation()
        .RunAsync(cancellationToken);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine($"Tests failed with exit code {exitCode}".Red());
        Environment.ExitCode = exitCode;
        return Unit.Value;
      }

      bool wroteCoverage = Directory
        .EnumerateFiles(coverageDirectory, "coverage.cobertura.xml", SearchOption.AllDirectories)
        .Any();
      if (!wroteCoverage)
      {
        Terminal.WriteErrorLine("Tests passed but wrote no coverage.cobertura.xml under artifacts/coverage.".Red());
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine("All tests passed.".Green());
      return Unit.Value;
    }
  }
}
