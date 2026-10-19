using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// The host routes of the roslynk command: <c>daemon</c> and the bare command run the shared daemon on the
/// port they are given, <c>stdio</c> bridges an MCP client to it with the published tools unchanged, and an
/// operation call with no daemon starts this same command as the daemon.
/// </summary>
public class HostRoutesTests
{
	private static readonly TimeSpan StartWindow = TimeSpan.FromSeconds(60);

	[Theory]
	[InlineData("daemon")]
	[InlineData("")]
	public async Task WhenTheDaemonRouteRuns_ThenItServesOperationsOnThePortItWasGiven(string verb)
	{
		int port = CliProcess.UnusedPort();
		string[] arguments = verb.Length == 0 ? [PortArgument(port)] : [verb, PortArgument(port)];

		// Roslynk__Port points elsewhere, so only the command-line setting can put the daemon on this port.
		using Process daemon = StartDaemon(arguments, CliProcess.UnusedPort());
		try
		{
			Assert.True(await CliProcess.WaitForListenerAsync(port, StartWindow), $"roslynk {verb} did not listen on {port}.");

			CliResult status = await CliProcess.RunAsync(["get_solution_status"], port);

			Assert.True(status.ExitCode == 0, status.StandardError);
			Assert.Equal("", status.StandardError);
		}
		finally
		{
			daemon.Kill(entireProcessTree: true);
		}
	}

	[Fact]
	public async Task WhenAnMcpClientRunsTheStdioRoute_ThenItSeesThePublishedToolsUnchanged()
	{
		int port = CliProcess.UnusedPort();
		using Process daemon = StartDaemon(["daemon", PortArgument(port)], port);
		try
		{
			Assert.True(await CliProcess.WaitForListenerAsync(port, StartWindow), $"roslynk daemon did not listen on {port}.");

			var transport = new StdioClientTransport(new StdioClientTransportOptions
			{
				Command = CliProcess.DotnetHost,
				Arguments = [CliProcess.CliAssembly, "stdio"],
				EnvironmentVariables = new Dictionary<string, string?> { ["Roslynk__Port"] = port.ToString(CultureInfo.InvariantCulture) },
				Name = "roslynk stdio",
			});
			await using McpClient client = await McpClient.CreateAsync(transport);

			IList<McpClientTool> tools = await client.ListToolsAsync();
			CallToolResult status = await client.CallToolAsync("get_solution_status", new Dictionary<string, object?>());

			Assert.Equal("Roslynk", client.ServerInfo.Name);
			Assert.Equal(Serialize(PublishedTools.Tools), Serialize(tools.Select(tool => tool.ProtocolTool)));
			Assert.NotEqual(true, status.IsError);
		}
		finally
		{
			daemon.Kill(entireProcessTree: true);
		}
	}

	[Fact]
	public async Task WhenNoDaemonListensAndStartIsOn_ThenTheCommandStartsItselfAsTheDaemonAndForwards()
	{
		if (!OperatingSystem.IsLinux())
			return; // The started daemon is detached; finding it again to stop it reads /proc.

		int port = CliProcess.UnusedPort();
		try
		{
			CliResult status = await CliProcess.RunAsync(
				["get_solution_status"],
				port,
				environment: new Dictionary<string, string> { ["Roslynk__Cli__AutoStart"] = "true" });

			Assert.True(status.ExitCode == 0, status.StandardError);
			Assert.Equal("", status.StandardError);
			Assert.True(await CliProcess.IsListeningAsync(port));
			Assert.Contains(StartedDaemons(port), commandLine => commandLine.Contains(Path.GetFullPath(CliProcess.CliAssembly), StringComparison.Ordinal));
		}
		finally
		{
			StopStartedDaemons(port);
		}
	}

	private static string PortArgument(int port) => "--Roslynk:Port=" + port.ToString(CultureInfo.InvariantCulture);

	private static Process StartDaemon(string[] arguments, int environmentPort)
	{
		Process daemon = Process.Start(CliProcess.StartInfo(arguments, environmentPort))
			?? throw new InvalidOperationException("Could not start roslynk.");

		// Drain the daemon's logging so a full pipe never blocks it.
		daemon.OutputDataReceived += (_, _) => { };
		daemon.ErrorDataReceived += (_, _) => { };
		daemon.BeginOutputReadLine();
		daemon.BeginErrorReadLine();
		return daemon;
	}

	private static string Serialize(IEnumerable<Tool> tools) =>
		JsonSerializer.Serialize(tools.OrderBy(tool => tool.Name, StringComparer.Ordinal), McpJsonUtilities.DefaultOptions);

	/// <summary>The command lines of the processes started with this port's argument.</summary>
	private static List<string> StartedDaemons(int port) =>
		DaemonProcesses(port).Select(entry => entry.CommandLine).ToList();

	private static void StopStartedDaemons(int port)
	{
		foreach ((int id, _) in DaemonProcesses(port))
		{
			try
			{
				using Process process = Process.GetProcessById(id);
				process.Kill(entireProcessTree: true);
			}
			catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
			{
				// Already gone.
			}
		}
	}

	private static IEnumerable<(int Id, string CommandLine)> DaemonProcesses(int port)
	{
		string argument = PortArgument(port);
		foreach (string folder in Directory.EnumerateDirectories("/proc"))
		{
			if (!int.TryParse(Path.GetFileName(folder), out int id))
				continue;

			string commandLine;
			try
			{
				commandLine = File.ReadAllText(Path.Combine(folder, "cmdline")).Replace('\0', ' ');
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
			{
				continue;
			}

			// The detaching shell carries the same argument; only the daemon itself runs the command.
			if (commandLine.Contains(argument, StringComparison.Ordinal) && !commandLine.StartsWith("/bin/sh", StringComparison.Ordinal))
				yield return (id, commandLine);
		}
	}
}
