using System.Diagnostics;
using System.Globalization;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// Builds the detached start of a daemon. Kept free of the CLI's other types so the tests can link this
/// file and check both launchers on any OS.
/// </summary>
internal static class DaemonStartInfo
{
	/// <summary>
	/// The command-line form of the daemon's <c>Roslynk:Port</c>. The port may come from the CLI's own
	/// appsettings.json, which the daemon never reads, so it is handed over on both launchers. The daemon's
	/// <c>WebApplication.CreateBuilder(args)</c> reads command-line arguments above its appsettings and
	/// environment, and unlike an environment block an argument survives the Windows shell-execute start.
	/// </summary>
	public static string PortArgument(int port) =>
		"--Roslynk:Port=" + port.ToString(CultureInfo.InvariantCulture);

	/// <param name="executable">The daemon executable, or the <c>dotnet</c> host.</param>
	/// <param name="assembly">For a framework-dependent daemon, the assembly <c>dotnet</c> runs.</param>
	/// <param name="workingDirectory">The daemon's folder, so it finds its own appsettings.json.</param>
	public static ProcessStartInfo Create(
		string executable,
		string? assembly,
		string workingDirectory,
		int port,
		string logFilePath,
		bool windows)
	{
		ProcessStartInfo startInfo;
		if (windows)
		{
			startInfo = new ProcessStartInfo(executable)
			{
				UseShellExecute = true,
				WindowStyle = ProcessWindowStyle.Hidden,
				WorkingDirectory = workingDirectory,
			};
		}
		else
		{
			// nohup + shell backgrounding reparents the daemon away from the CLI, and the redirect keeps
			// its console output out of the CLI's stdout. $0 is the log; "$@" is the daemon command line.
			startInfo = new ProcessStartInfo("/bin/sh")
			{
				ArgumentList =
				{
					"-c",
					"""nohup "$@" >>"$0" 2>&1 </dev/null &""",
					logFilePath,
					executable,
				},
				UseShellExecute = false,
				WorkingDirectory = workingDirectory,
			};
		}

		if (assembly is not null)
			startInfo.ArgumentList.Add(assembly);
		startInfo.ArgumentList.Add(PortArgument(port));
		return startInfo;
	}
}
