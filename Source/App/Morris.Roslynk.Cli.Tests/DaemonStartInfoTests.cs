using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Morris.Roslynk.Cli.Forwarding;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// The resolved port reaches a daemon the CLI starts, on both launchers: the port may exist only in the
/// CLI's own appsettings.json, which the daemon never reads. <see cref="DaemonStartInfo"/> is linked in.
/// </summary>
public class DaemonStartInfoTests
{
	private const int Port = 6599;
	private const string Log = "/tmp/roslynk/daemon.log";
	private const string WorkingDirectory = "/opt/roslynk";

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void WhenAnApphostIsStarted_ThenTheResolvedPortIsItsLastArgument(bool windows)
	{
		ProcessStartInfo startInfo = DaemonStartInfo.Create("/opt/roslynk/roslynk", null, WorkingDirectory, Port, Log, windows);

		Assert.Equal(DaemonStartInfo.PortArgument(Port), startInfo.ArgumentList[^1]);
		Assert.Equal("--Roslynk:Port=6599", startInfo.ArgumentList[^1]);
		Assert.Equal(WorkingDirectory, startInfo.WorkingDirectory);
	}

	[Fact]
	public void WhenStartedOnWindows_ThenTheDaemonIsShellExecutedHiddenWithItsAssemblyAndPort()
	{
		ProcessStartInfo startInfo = DaemonStartInfo.Create("dotnet", "/opt/roslynk/Morris.Roslynk.Mcp.dll", WorkingDirectory, Port, Log, windows: true);

		Assert.True(startInfo.UseShellExecute);
		Assert.Equal(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
		Assert.Equal("dotnet", startInfo.FileName);
		Assert.Equal(["/opt/roslynk/Morris.Roslynk.Mcp.dll", "--Roslynk:Port=6599"], startInfo.ArgumentList);
	}

	[Fact]
	public void WhenStartedOnUnix_ThenTheShellRunsTheDaemonWithItsAssemblyAndPortIntoTheLog()
	{
		ProcessStartInfo startInfo = DaemonStartInfo.Create("dotnet", "/opt/roslynk/Morris.Roslynk.Mcp.dll", WorkingDirectory, Port, Log, windows: false);

		Assert.False(startInfo.UseShellExecute);
		Assert.Equal("/bin/sh", startInfo.FileName);
		Assert.Equal("-c", startInfo.ArgumentList[0]);
		Assert.Equal([Log, "dotnet", "/opt/roslynk/Morris.Roslynk.Mcp.dll", "--Roslynk:Port=6599"], startInfo.ArgumentList.Skip(2));
	}

	[Fact]
	public async Task WhenTheUnixStartRuns_ThenTheDaemonReceivesItsArgumentsVerbatimAndWritesToTheLog()
	{
		if (OperatingSystem.IsWindows())
			return;

		string folder = Path.Combine(Path.GetTempPath(), $"roslynk-start-{Guid.NewGuid():N}");
		Directory.CreateDirectory(folder);
		try
		{
			// A stand-in daemon: one line per argument into the log, so spaces and order are visible.
			string daemon = Path.Combine(folder, "fake daemon");
			await File.WriteAllTextAsync(daemon, "#!/bin/sh\nprintf '%s\\n' \"$@\"\n");
			File.SetUnixFileMode(daemon, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			string log = Path.Combine(folder, "daemon log.txt");

			using (Process? shell = Process.Start(DaemonStartInfo.Create(daemon, "/opt/a b/Morris.Roslynk.Mcp.dll", folder, Port, log, windows: false)))
				await shell!.WaitForExitAsync();

			string[] expected = ["/opt/a b/Morris.Roslynk.Mcp.dll", "--Roslynk:Port=6599"];
			string[] lines = [];
			for (int attempt = 0; attempt < 100 && !lines.SequenceEqual(expected); attempt++)
			{
				await Task.Delay(50);
				lines = File.Exists(log) ? await File.ReadAllLinesAsync(log) : [];
			}

			Assert.Equal(expected, lines);
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public void WhenTheDaemonBuildsItsHost_ThenThePortArgumentIsItsRoslynkPort()
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder([DaemonStartInfo.PortArgument(Port)]);

		Assert.Equal(Port, builder.Configuration.GetValue<int>("Roslynk:Port"));
	}
}
