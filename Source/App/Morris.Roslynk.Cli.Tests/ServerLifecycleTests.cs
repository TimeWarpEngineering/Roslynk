using System.Diagnostics;
using ModelContextProtocol.Client;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Self-start parity: an operation call or <c>roslynk stdio</c> with no server starts one, and when none can be
/// started the call is one line on stderr with exit 69 (never a slow local run).
/// </summary>
public class ServerLifecycleTests
{
	[Fact]
	public async Task WhenNoServerRuns_ThenAnOperationCallStartsOne()
	{
		await using ServerSocket socket = new();
		Assert.False(await socket.IsServingAsync());

		CliResult result = await CliProcess.RunAsync(["get_solution_status"], socket);

		Assert.True(result.ExitCode == 0, result.StandardError);
		Assert.True(await socket.IsServingAsync());
	}

	[Fact]
	public async Task WhenNoServerRuns_ThenStdioStartsOneBeforeServing()
	{
		await using ServerSocket socket = new();

		await using (McpClient client = await Mcp.ConnectAsync(socket))
		{
			Assert.True(await socket.IsServingAsync());
			(string text, bool isError) = await client.CallAsync("get_solution_status", []);
			Assert.False(isError, text);
		}
	}

	[UnixFact]
	public async Task WhenNoServerCanStart_ThenAnOperationCallIsOneLineAndExit69()
	{
		await using ServerSocket socket = new();
		MakeUnsafe(socket);

		CliResult result = await CliProcess.RunAsync(["get_solution_status"], socket);

		Assert.Equal(69, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		Assert.Single(result.StandardError.TrimEnd('\n').Split('\n'));
		Assert.StartsWith("Error:", result.StandardError);
	}

	[UnixFact]
	public async Task WhenNoServerCanStart_ThenStdioIsOneLineAndExit69BeforeAnyProtocol()
	{
		await using ServerSocket socket = new();
		MakeUnsafe(socket);

		using Process process = Process.Start(CliProcess.StartInfo(["stdio"], socket))!;
		Task<string> stdout = process.StandardOutput.ReadToEndAsync();
		Task<string> stderr = process.StandardError.ReadToEndAsync();
		await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(60));

		Assert.Equal(69, process.ExitCode);
		Assert.Equal("", await stdout);
		Assert.Single((await stderr).TrimEnd('\n').Split('\n'));
	}

	/// <summary>
	/// A socket directory others can write is refused by client and server alike (it could be another user's
	/// server), the one start failure a test can arrange without breaking the build.
	/// </summary>
	private static void MakeUnsafe(ServerSocket socket)
	{
		if (!OperatingSystem.IsWindows())
			File.SetUnixFileMode(socket.Directory, (UnixFileMode)0b111_111_111);
	}
}

/// <summary>A fact that needs Unix file modes (socket directory permissions are a Unix check).</summary>
public sealed class UnixFactAttribute : FactAttribute
{
	public UnixFactAttribute()
	{
		if (OperatingSystem.IsWindows())
			Skip = "Socket directory permissions are a Unix check.";
	}
}
