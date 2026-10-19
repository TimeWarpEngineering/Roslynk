using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// Makes sure a Roslynk daemon listens on the configured port, with the probe, detached start and 30 s
/// startup window of the host's <c>DaemonLauncher</c>. The CLI is a different executable, so it cannot
/// re-run itself as the daemon; it starts, in order, <c>Roslynk:Cli:Daemon</c>, a daemon published beside
/// it, the sibling <c>Morris.Roslynk.Mcp</c> build of this checkout, or the <c>roslynk</c> tool on PATH.
/// </summary>
internal static class DaemonStarter
{
	private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
	private static readonly TimeSpan ProbeTimeout = TimeSpan.FromMilliseconds(500);

	private const string StartHint =
		"Start it with 'roslynk' (dotnet tool install --global Roslynk), or set Roslynk:Cli:Daemon to the daemon to start.";

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
			return $"roslynk-cli: no Roslynk daemon is listening on port {settings.Port} and Roslynk:Cli:AutoStart is false. {StartHint}";

		DaemonCommand? command = Locate(settings);
		if (command is null)
			return $"roslynk-cli: no Roslynk daemon is listening on port {settings.Port} and none was found to start. {StartHint}";

		try
		{
			StartDetached(command, settings.Port);
		}
		catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
		{
			return $"roslynk-cli: could not start the Roslynk daemon '{command.Display}': {DaemonForwarder.OneLine(exception.Message)}. {StartHint}";
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
				return $"roslynk-cli: the Roslynk daemon '{command.Display}' did not start listening on port {settings.Port} " +
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

		string baseDirectory = AppContext.BaseDirectory;
		foreach (string beside in new[] { "roslynk", "Morris.Roslynk.Mcp" })
			if (FromDirectory(baseDirectory, beside) is DaemonCommand published)
				return published;

		// bin/<Configuration>/<TargetFramework>/ of Morris.Roslynk.Cli maps onto the same folders of
		// Morris.Roslynk.Mcp, so a CLI built from this checkout starts the daemon built beside it.
		DirectoryInfo output = new(Path.TrimEndingDirectorySeparator(baseDirectory));
		if (output.Parent?.Parent?.Parent?.Parent is DirectoryInfo appFolder && output.Parent.Parent.Name == "bin")
		{
			string sibling = Path.Combine(appFolder.FullName, "Morris.Roslynk.Mcp", "bin", output.Parent.Name, output.Name);
			if (FromDirectory(sibling, "Morris.Roslynk.Mcp") is DaemonCommand built)
				return built;
		}

		return FromPathVariable("roslynk");
	}

	private static DaemonCommand? FromDirectory(string directory, string name)
	{
		string apphost = Path.Combine(directory, OperatingSystem.IsWindows() ? name + ".exe" : name);
		if (File.Exists(apphost))
			return new DaemonCommand(apphost, null);

		string assembly = Path.Combine(directory, name + ".dll");
		return File.Exists(assembly) && File.Exists(Path.Combine(directory, name + ".runtimeconfig.json"))
			? new DaemonCommand(DotnetHost(), assembly)
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
