using System.Diagnostics;
using ModelContextProtocol.Client;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 1: MCP sessions and CLI calls share one warm server and run concurrently. A long
/// <c>get_diagnostics</c> (the first, analyzer-loading build) does not hold up a <c>get_symbol</c> from another
/// session or the CLI.
/// </summary>
public class ConcurrencyTests : IClassFixture<ServedSolution>
{
	private readonly ServedSolution Served;

	public ConcurrencyTests(ServedSolution served)
	{
		Served = served;
	}

	[Fact]
	public async Task WhenTwoSessionsAndACliCallRunTogether_ThenTheyShareOneWarmServer()
	{
		string status = (await Served.RunAsync(["serve", "--status"])).StandardOutput;
		await using McpClient first = await Served.ConnectAsync();
		await using McpClient second = await Served.ConnectAsync();
		Dictionary<string, object?> arguments = new() { ["solutionId"] = Served.SolutionId, ["symbolName"] = ServedSolution.Calculator };

		Task<(string Text, bool IsError)> fromFirst = first.CallAsync("get_symbol", arguments);
		Task<(string Text, bool IsError)> fromSecond = second.CallAsync("get_symbol", arguments);
		Task<CliResult> fromCli = Served.RunAsync(["get_symbol", "--solutionId", Served.SolutionId, "--symbolName", ServedSolution.Calculator]);
		await Task.WhenAll(fromFirst, fromSecond, fromCli);

		Assert.False((await fromFirst).IsError);
		Assert.Equal((await fromFirst).Text, (await fromSecond).Text);
		Assert.Equal((await fromFirst).Text, (await fromCli).StandardOutput.TrimEnd('\n'));
		Assert.Equal(status, (await Served.RunAsync(["serve", "--status"])).StandardOutput);
	}

	[Fact]
	public async Task WhenALongDiagnosticsBuildRuns_ThenAConcurrentGetSymbolIsNotBlocked()
	{
		await using McpClient diagnosing = await Served.ConnectAsync();
		await using McpClient reading = await Served.ConnectAsync();
		Dictionary<string, object?> symbol = new() { ["solutionId"] = Served.SolutionId, ["symbolName"] = ServedSolution.Calculator };
		await reading.CallAsync("get_symbol", symbol); // Warm this session's path to the server.

		var clock = Stopwatch.StartNew();
		Task<(string Text, bool IsError)> diagnostics = diagnosing.CallAsync("get_diagnostics", new() { ["solutionId"] = Served.SolutionId, ["includeAnalyzers"] = true });
		await Task.Delay(200);

		(string text, bool isError) = await reading.CallAsync("get_symbol", symbol);
		TimeSpan symbolDone = clock.Elapsed;
		bool diagnosticsStillRunning = !diagnostics.IsCompleted;
		(string diagnosticsText, bool diagnosticsFailed) = await diagnostics;
		TimeSpan diagnosticsDone = clock.Elapsed;

		Assert.False(isError, text);
		Assert.False(diagnosticsFailed, diagnosticsText);
		Assert.StartsWith("errors=", diagnosticsText);
		Assert.True(
			diagnosticsStillRunning,
			$"get_symbol finished at {symbolDone.TotalMilliseconds:F0} ms, after get_diagnostics ({diagnosticsDone.TotalMilliseconds:F0} ms).");
	}
}
