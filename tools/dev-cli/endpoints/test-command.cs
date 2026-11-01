#region Purpose
// Test command - runs the Roslynk test suites via the solution
#endregion

using TimeWarp.Amuru;
using TimeWarp.Nuru;
using static DevCli.ProcessHelpers;

namespace DevCli.Endpoints;

[NuruRoute("test", Description = "Run all tests in source/Morris.Roslynk.slnx in Release")]
public sealed class TestCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    public async Task<Unit> Handle(TestCommand command, CancellationToken cancellationToken)
    {
      await TimeWarpTerminal.Default.WriteLineAsync("🧪 Running Roslynk tests...");

      string? repoRoot = Git.FindRoot();
      if (repoRoot == null)
      {
        await TimeWarpTerminal.Default.WriteLineAsync("❌ Not in a git repository");
        Environment.Exit(1);
      }

      string solutionPath = Path.Combine(repoRoot, "source", "Morris.Roslynk.slnx");

      if (!File.Exists(solutionPath))
      {
        await TimeWarpTerminal.Default.WriteLineAsync($"❌ Solution not found: {solutionPath}");
        Environment.Exit(1);
      }

      int exitCode = await RunProcessAsync("dotnet", $"test \"{solutionPath}\" -c Release --verbosity minimal");

      if (exitCode != 0)
      {
        await TimeWarpTerminal.Default.WriteLineAsync($"❌ Tests failed with exit code {exitCode}");
        Environment.Exit(1);
      }

      await TimeWarpTerminal.Default.WriteLineAsync("✅ All tests passed!");

      return Unit.Value;
    }
  }
}
