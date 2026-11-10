#region Purpose
// verify-samples: the dev-cli-capabilities audit's standard command; Roslynk has no samples
#endregion
#region Design
// Roslynk ships no samples/ tree (its fixtures under source/TestFixtures are built by the tests),
// so there is nothing to compile. The command says so and succeeds. If a samples/ directory is
// ever added, this command reports it as unverified and fails instead of passing silently.
#endregion

namespace DevCli.Endpoints;

[NuruRoute("verify-samples", Description = "Verify code samples compile (Roslynk has none)")]
public sealed class VerifySamplesCommand : ICommand<Unit>
{
  internal sealed class Handler : IRequestHandler<VerifySamplesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken cancellationToken)
    {
      string? repoRoot = Git.FindRoot();
      if (repoRoot is not null && Directory.Exists(Path.Combine(repoRoot, "samples")))
      {
        Terminal.WriteErrorLine("Error: samples/ exists but verify-samples has no build for it yet. Add one in tools/dev-cli/endpoints/verify-samples-command.cs.");
        Environment.ExitCode = 1;
        return Unit.ValueTask;
      }

      Terminal.WriteLine("No samples to verify: Roslynk has no samples/ directory.");
      return Unit.ValueTask;
    }
  }
}
