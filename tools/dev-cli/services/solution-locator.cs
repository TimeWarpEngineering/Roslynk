#region Purpose
// Finds source/Morris.Roslynk.slnx from the git repository root
#endregion

namespace DevCli;

internal static class SolutionLocator
{
  public static string? Find(ITerminal terminal)
  {
    string? repoRoot = Git.FindRoot();
    if (repoRoot is null)
    {
      terminal.WriteErrorLine("Error: not in a git repository.");
      return null;
    }

    string solutionPath = Path.Combine(repoRoot, "source", "Morris.Roslynk.slnx");
    if (!File.Exists(solutionPath))
    {
      terminal.WriteErrorLine($"Error: solution not found: {solutionPath}");
      return null;
    }

    return solutionPath;
  }
}
