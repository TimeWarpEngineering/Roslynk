using System.Text.Json;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 3 against the engine itself: the CLI prints exactly the text an MCP client reads for the
/// same call, for a success, an <c>error=</c> problem, a Stale write and a <c>--json-args</c> batch.
/// </summary>
public class RealDaemonTests : IClassFixture<RealDaemon>
{
	private readonly RealDaemon Daemon;

	public RealDaemonTests(RealDaemon daemon)
	{
		Daemon = daemon;
	}

	[Fact]
	public async Task WhenAQuerySucceeds_ThenStdoutIsTheMcpText()
	{
		string mcp = await Daemon.CallAsync("get_symbol", new() { ["solutionId"] = Daemon.SolutionId, ["symbolName"] = "SimpleLibrary.Calculator" });

		CliResult cli = await CliProcess.RunAsync(
			["get_symbol", "--solutionId", Daemon.SolutionId, "--symbolName", "SimpleLibrary.Calculator"],
			Daemon.Port);

		Assert.DoesNotContain("error=", mcp, StringComparison.Ordinal);
		Assert.Equal(0, cli.ExitCode);
		Assert.Equal(mcp, cli.StandardOutput);
		Assert.Equal("", cli.StandardError);
	}

	[Fact]
	public async Task WhenAQueryFails_ThenStderrIsTheMcpOutlineAndTheExitCodeIsNotFound()
	{
		string mcp = await Daemon.CallAsync("get_symbol", new() { ["solutionId"] = Daemon.SolutionId, ["symbolName"] = "NoSuchTypeAnywhere" });

		CliResult cli = await CliProcess.RunAsync(
			["get_symbol", "--solutionId", Daemon.SolutionId, "--symbolName", "NoSuchTypeAnywhere"],
			Daemon.Port);

		Assert.StartsWith("error=NotFound\n", mcp, StringComparison.Ordinal);
		Assert.Equal(3, cli.ExitCode);
		Assert.Equal("", cli.StandardOutput);
		Assert.Equal(mcp, cli.StandardError);
	}

	[Fact]
	public async Task WhenAPatchIsBasedOnAnOldVersion_ThenStderrIsTheStaleOutlineAndTheExitCodeIsSix()
	{
		const string patch = "--- a/SimpleLibrary/Calculator.cs\n+++ b/SimpleLibrary/Calculator.cs\n@@ -7 +7 @@\n-\t\treturn a + b;\n+\t\treturn b + a;\n";
		var baseVersions = new[] { new { path = "SimpleLibrary/Calculator.cs", version = new string('0', 64) } };
		string mcp = await Daemon.CallAsync("apply_patch", new()
		{
			["solutionId"] = Daemon.SolutionId,
			["patch"] = patch,
			["baseVersions"] = baseVersions,
			["checkOnly"] = true,
		});

		string jsonArgs = JsonSerializer.Serialize(new { solutionId = Daemon.SolutionId, patch, baseVersions });
		CliResult cli = await CliProcess.RunAsync(["apply_patch", "--checkOnly", "--json-args", "-"], Daemon.Port, standardInput: jsonArgs);

		Assert.StartsWith("error=Stale\n", mcp, StringComparison.Ordinal);
		Assert.Equal(6, cli.ExitCode);
		Assert.Equal("", cli.StandardOutput);
		Assert.Equal(mcp, cli.StandardError);
		Assert.Equal("namespace SimpleLibrary;", File.ReadLines(Path.Combine(Daemon.SolutionDirectory, "SimpleLibrary", "Calculator.cs")).First());
	}

	[Fact]
	public async Task WhenABatchComesThroughJsonArgs_ThenStdoutIsTheMcpText()
	{
		var operations = new object[]
		{
			new { tool = "get_symbol", arguments = new { symbolName = "SimpleLibrary.Calculator" } },
			new { tool = "find_references", arguments = new { symbolName = "SimpleLibrary.Calculator.Add" } },
		};
		string mcp = await Daemon.CallAsync("multi_query", new() { ["solutionId"] = Daemon.SolutionId, ["operations"] = operations });

		CliResult cli = await CliProcess.RunAsync(
			["multi_query", "--json-args", JsonSerializer.Serialize(new { solutionId = Daemon.SolutionId, operations })],
			Daemon.Port);

		// Each batch frames its slots with a fresh random boundary; everything else is the same text.
		Assert.DoesNotContain("error=", mcp, StringComparison.Ordinal);
		Assert.Equal(0, cli.ExitCode);
		Assert.Equal(WithoutBoundary(mcp), WithoutBoundary(cli.StandardOutput));
	}

	private static string WithoutBoundary(string batch)
	{
		string boundary = batch.Split('\n').Single(line => line.StartsWith("boundary=", StringComparison.Ordinal))["boundary=".Length..];
		return batch.Replace(boundary, "<boundary>", StringComparison.Ordinal);
	}
}
