#region Purpose
// Test command - runs every test project in source/Morris.Roslynk.slnx in Release
#endregion
#region Design
// Streams dotnet output and reports failure through Environment.ExitCode (never
// Environment.Exit), so the workflow command can run it as a pipeline step.
#endregion

namespace DevCli.Endpoints;

[NuruRoute("test", Description = "Run all tests in source/Morris.Roslynk.slnx in Release")]
public sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(TestCommand command, CancellationToken cancellationToken)
    {
      string? solutionPath = SolutionLocator.Find(Terminal);
      if (solutionPath is null)
      {
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine("Running Roslynk tests...");

      int exitCode = await Shell.Builder("dotnet")
        .WithArguments("test", solutionPath, "--configuration", "Release", "--verbosity", "minimal")
        .WithNoValidation()
        .RunAsync(cancellationToken);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine($"Tests failed with exit code {exitCode}".Red());
        Environment.ExitCode = exitCode;
        return Unit.Value;
      }

      Terminal.WriteLine("All tests passed.".Green());
      return Unit.Value;
    }
  }
}
