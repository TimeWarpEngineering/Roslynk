#region Purpose
// Build command - builds source/Morris.Roslynk.slnx in Release
#endregion
#region Design
// Streams dotnet output and reports failure through Environment.ExitCode (never
// Environment.Exit), so the workflow command can run it as a pipeline step.
#endregion

namespace DevCli.Endpoints;

[NuruRoute("build", Description = "Build source/Morris.Roslynk.slnx in Release")]
public sealed class BuildCommand : ICommand<Unit>
{
  internal sealed class Handler : IRequestHandler<BuildCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BuildCommand command, CancellationToken cancellationToken)
    {
      string? solutionPath = SolutionLocator.Find(Terminal);
      if (solutionPath is null)
      {
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      Terminal.WriteLine("Building Roslynk...");

      int exitCode = await Shell.Builder("dotnet")
        .WithArguments("build", solutionPath, "--configuration", "Release", "--verbosity", "minimal")
        .WithNoValidation()
        .RunAsync(cancellationToken);

      if (exitCode != 0)
      {
        Terminal.WriteErrorLine($"Build failed with exit code {exitCode}".Red());
        Environment.ExitCode = exitCode;
        return Unit.Value;
      }

      Terminal.WriteLine("Build completed successfully.".Green());
      return Unit.Value;
    }
  }
}
