using ModelContextProtocol.Client;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 4, end to end: an MCP cancel or a CLI client that goes away stops waiting for a long call, and
/// the server keeps serving. That the cancellation reaches the engine is <see cref="HandlerTests"/>; that Nuru
/// cancels the call's token in the server when its client cancels or disconnects is Nuru's own contract.
/// </summary>
public class CancellationTests : IClassFixture<ServedSolution>
{
	private readonly ServedSolution Served;

	public CancellationTests(ServedSolution served)
	{
		Served = served;
	}

	[Fact]
	public async Task WhenAnMcpCallIsCancelled_ThenItStopsAndTheSessionAndServerKeepServing()
	{
		await using McpClient client = await Served.ConnectAsync();
		using CancellationTokenSource cancel = new(TimeSpan.FromMilliseconds(200));

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			client.CallAsync("get_diagnostics", new() { ["solutionId"] = Served.SolutionId, ["includeAnalyzers"] = true }, cancel.Token));

		(string text, bool isError) = await client.CallAsync("get_symbol", new() { ["solutionId"] = Served.SolutionId, ["symbolName"] = ServedSolution.Calculator });
		Assert.False(isError, text);
		(string diagnostics, bool failed) = await client.CallAsync("get_diagnostics", new() { ["solutionId"] = Served.SolutionId });
		Assert.False(failed, diagnostics);
		Assert.StartsWith("errors=", diagnostics);
	}

	[Fact]
	public async Task WhenACliClientIsKilledMidCall_ThenTheServerKeepsServing()
	{
		using System.Diagnostics.Process process = System.Diagnostics.Process.Start(
			CliProcess.StartInfo(["get_diagnostics", "--solutionId", Served.SolutionId, "--includeAnalyzers", "true"], Served.Socket))!;
		await Task.Delay(300);
		process.Kill(entireProcessTree: true);
		await process.WaitForExitAsync();

		CliResult result = await Served.RunAsync(["get_symbol", "--solutionId", Served.SolutionId, "--symbolName", ServedSolution.Calculator]);
		Assert.True(result.ExitCode == 0, result.StandardError);
	}
}
