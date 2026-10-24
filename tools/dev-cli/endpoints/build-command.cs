#region Purpose
// Build command - builds source/Morris.Roslynk.slnx
#endregion

using TimeWarp.Amuru;
using TimeWarp.Nuru;
using static DevCli.ProcessHelpers;

namespace DevCli.Endpoints;

[NuruRoute("build", Description = "Build source/Morris.Roslynk.slnx in Release")]
public sealed class BuildCommand : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<BuildCommand, Unit>
  {
    public async Task<Unit> Handle(BuildCommand command, CancellationToken cancellationToken)
    {
      await TimeWarpTerminal.Default.WriteLineAsync("Building Roslynk...");

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

      int exitCode = await RunProcessAsync("dotnet", $"build \"{solutionPath}\" -c Release --verbosity minimal");

      if (exitCode != 0)
      {
        await TimeWarpTerminal.Default.WriteLineAsync($"❌ Build failed with exit code {exitCode}");
        Environment.Exit(1);
      }

      await TimeWarpTerminal.Default.WriteLineAsync("✅ Build completed successfully!");

      return Unit.Value;
    }
  }
}
