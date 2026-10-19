using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// Makes sure a Roslynk daemon listens on the configured port, with the probe, detached start and 30 s
/// startup window of the host's <c>DaemonLauncher</c>. The <c>roslynk</c> command is the daemon too, so it
/// starts itself (the bare command is the daemon route), unless <c>Roslynk:Cli:Daemon</c> names another one.
/// </summary>
internal static class DaemonStarter
{
	private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(500);

	private const string StartHint =
		"Start it with 'roslynk daemon', or set Roslynk:Cli:Daemon to the daemon to start.";

	public static string LogFilePath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
		"Roslynk",
		"daemon.log");

	/// <summary>Returns null when a daemon listens, otherwise the one line that explains how to start one.</summary>
	public static async Task<string?> EnsureRunningAsync(CliSettings settings, CancellationToken cancellationToken)
	{
		if (await IsListeningAsync(settings.Port, cancellationToken))
			return null;

		if (!settings.AutoStart)
			return $"roslynk: no Roslynk daemon is listening on port {settings.Port} and Roslynk:Cli:AutoStart is false. {StartHint}";

		DaemonCommand? command = Locate(settings);
		if (command is null)
			return $"roslynk: no Roslynk daemon is listening on port {settings.Port} and none was found to start. {StartHint}";

		try
		{
			StartDetached(command, settings.Port);
		}
		catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
		{
			return $"roslynk: could not start the Roslynk daemon '{command.Display}': {DaemonForwarder.OneLine(exception.Message)}. {StartHint}";
		}

		using var startupWindow = new CancellationTokenSource(StartupTimeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(startupWindow.Token, cancellationToken);
		while (!await IsListeningAsync(settings.Port, linked.Token))
		{
			try
			{
				await Task.Delay(200, linked.Token);
			}
			catch (OperationCanceledException) when (startupWindow.IsCancellationRequested)
			{
				return $"roslynk: the Roslynk daemon '{command.Display}' did not start listening on port {settings.Port} " +
					$"within {StartupTimeout.TotalSeconds:0}s; see '{LogFilePath}'.";
			}
		}

		return null;
	}

	private static async Task<bool> IsListeningAsync(int port, CancellationToken cancellationToken)
	{
		using var probe = new TcpClient();
		try
		{
			await probe.ConnectAsync(IPAddress.Loopback, port, cancellationToken).AsTask().WaitAsync(ProbeTimeout, cancellationToken);
			return true;
		}
		catch (Exception exception) when (exception is SocketException or TimeoutException)
		{
			return false;
		}
	}

	/// <summary>An executable plus, for a framework-dependent <c>.dll</c>, the assembly <c>dotnet</c> runs.</summary>
	internal sealed record DaemonCommand(string Executable, string? Assembly)
	{
		public string Display => Assembly is null ? Executable : $"{Executable} {Assembly}";

		public string WorkingDirectory => Path.GetDirectoryName(Assembly ?? Executable) ?? AppContext.BaseDirectory;

		public static DaemonCommand? FromPath(string path)
		{
			if (!File.Exists(path))
				return null;

			return path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
				? new DaemonCommand(DotnetHost(), Path.GetFullPath(path))
				: new DaemonCommand(Path.GetFullPath(path), null);
		}
	}

	internal static DaemonCommand? Locate(CliSettings settings)
	{
		if (settings.Daemon is { Length: > 0 } configured)
			return DaemonCommand.FromPath(configured) ?? FromPathVariable(configured);

		return Self();
	}

	/// <summary>
	/// This process as a daemon command: the apphost re-runs itself, while under the <c>dotnet</c> host (dnx,
	/// <c>dotnet tool exec</c>, <c>dotnet roslynk.dll</c>) the daemon is <c>dotnet &lt;entry assembly&gt;</c>.
	/// </summary>
	private static DaemonCommand? Self()
	{
		if (Environment.ProcessPath is not { Length: > 0 } executable)
			return null;

		if (!Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
			return new DaemonCommand(executable, null);

		return System.Reflection.Assembly.GetEntryAssembly()?.Location is { Length: > 0 } entry
			? new DaemonCommand(executable, entry)
			: null;
	}

	private static DaemonCommand? FromPathVariable(string name)
	{
		string fileName = OperatingSystem.IsWindows() && !name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name + ".exe" : name;
		IEnumerable<string> folders = (Environment.GetEnvironmentVariable("PATH") ?? "")
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
			.Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools"));

		foreach (string folder in folders)
		{
			string candidate = Path.Combine(folder, fileName);
			if (File.Exists(candidate))
				return new DaemonCommand(candidate, null);
		}

		return null;
	}

	private static string DotnetHost() =>
		Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

	private static void StartDetached(DaemonCommand command, int port)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);

		ProcessStartInfo startInfo = DaemonStartInfo.Create(
			command.Executable,
			command.Assembly,
			command.WorkingDirectory,
			port,
			LogFilePath,
			OperatingSystem.IsWindows());

		// Two CLIs racing to start a daemon is benign: the loser fails to bind the port and exits.
		using Process? started = Process.Start(startInfo);
	}
}
