extern alias host;

using System.Runtime.CompilerServices;
using RoslynkDaemon = host::Morris.Roslynk.Mcp.Hosting.RoslynkDaemon;
using StdioBridge = host::Morris.Roslynk.Mcp.Hosting.StdioBridge;

namespace Morris.Roslynk.Cli.Hosting;

/// <summary>
/// The two host routes of the <c>roslynk</c> command. This is the only type that names the MCP host, and its
/// methods are not inlined, so the host and engine assemblies load only when one of these routes runs, never
/// for an operation call or <c>--capabilities</c>.
/// </summary>
internal static class HostEntry
{
	/// <summary><c>roslynk stdio</c>: the MCP stdio bridge to the shared daemon, starting the daemon when needed.</summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static Task RunStdioBridgeAsync() => StdioBridge.RunAsync();

	/// <summary>
	/// <c>roslynk</c> or <c>roslynk daemon</c>: the shared HTTP daemon, configured by the arguments of this
	/// process. Nuru takes <c>--Section:Key=value</c> arguments as its own configuration overrides and does not
	/// hand them to the route, so the daemon reads the process command line itself.
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void RunDaemon() => RoslynkDaemon.Run(DaemonArguments(Environment.GetCommandLineArgs()));

	/// <summary>The command line without the program path and the <c>daemon</c> verb.</summary>
	internal static string[] DaemonArguments(string[] commandLine) =>
		commandLine.AsSpan(Math.Min(1, commandLine.Length)) is ["daemon", .. var settings] ? settings.ToArray() : commandLine.Skip(1).ToArray();
}
