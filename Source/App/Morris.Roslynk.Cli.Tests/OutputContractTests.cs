using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 3 against a stub daemon: the success text is stdout byte for byte, an <c>error=</c> outline
/// is stderr byte for byte with an exit code from the documented table, and an unreachable daemon is one
/// line on stderr with exit 69. <see cref="RealDaemonTests"/> repeats the comparison against the engine.
/// </summary>
public class OutputContractTests : IClassFixture<StubDaemon>
{
	private readonly StubDaemon Daemon;

	public OutputContractTests(StubDaemon daemon)
	{
		Daemon = daemon;
		Daemon.Reset();
	}

	[Theory]
	[InlineData("project=SimpleLibrary\npath=SimpleLibrary/Calculator.cs\nloc=3:1\n\npublic class Calculator")]
	[InlineData("solutionId=s1\nstatus=Building\n")]
	[InlineData("")]
	public async Task WhenTheDaemonSucceeds_ThenStdoutIsTheMcpTextExactly(string text)
	{
		Daemon.Respond = _ => StubDaemon.Text(text);

		CliResult result = await CliProcess.RunAsync(["get_symbol", "--solutionId", "s1", "--symbolName", "Calculator"], Daemon.Port);

		Assert.Equal(0, result.ExitCode);
		Assert.Equal(text, result.StandardOutput);
		Assert.Equal("", result.StandardError);
	}

	[Theory]
	[InlineData("Invalid", 2)]
	[InlineData("NotFound", 3)]
	[InlineData("Ambiguous", 4)]
	[InlineData("Indexing", 5)]
	[InlineData("Stale", 6)]
	[InlineData("Conflict", 7)]
	[InlineData("NotSupported", 8)]
	[InlineData("Faulted", 9)]
	[InlineData("Truncated", 10)]
	[InlineData("SomethingNew", 1)]
	public async Task WhenTheDaemonReturnsAProblem_ThenStderrIsTheOutlineAndTheExitCodeFollowsTheTable(string code, int exitCode)
	{
		string text = $"error={code}\nerrorMessage=Symbol 'Calc' matched 2 symbols.\ncandidate=A.Calc\ncandidate=B.Calc\n";
		Daemon.Respond = _ => StubDaemon.Text(text, isError: true);

		CliResult result = await CliProcess.RunAsync(["get_symbol", "--solutionId", "s1", "--symbolName", "Calc"], Daemon.Port);

		Assert.Equal(exitCode, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		Assert.Equal(text, result.StandardError);
	}

	[Fact]
	public async Task WhenAWriteToolReportsStale_ThenStderrIsTheOutlineAndTheExitCodeIsSix()
	{
		string text = "error=Stale\nerrorMessage=Files changed on disk since they were read; re-read them and retry.\nstale=SimpleLibrary/Calculator.cs\n";
		Daemon.Respond = _ => StubDaemon.Text(text, isError: true);

		CliResult result = await CliProcess.RunAsync(
			["rename_symbol", "--solutionId", "s1", "--symbolName", "Calculator.Add", "--newName", "Sum"],
			Daemon.Port);

		Assert.Equal(6, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		Assert.Equal(text, result.StandardError);
		Assert.Equal("rename_symbol", Assert.Single(Daemon.Calls).Name);
	}

	[Fact]
	public async Task WhenNoDaemonListensAndStartIsOff_ThenOneLineSaysHowToStartItAndTheExitCodeIs69()
	{
		CliResult result = await CliProcess.RunAsync(["get_symbol", "--solutionId", "s1", "--symbolName", "Calculator"], CliProcess.UnusedPort());

		Assert.Equal(69, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		string line = Assert.Single(result.StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries));
		Assert.EndsWith("\n", result.StandardError, StringComparison.Ordinal);
		Assert.Contains("no Roslynk daemon is listening", line, StringComparison.Ordinal);
		Assert.Contains("Start it with 'roslynk daemon'", line, StringComparison.Ordinal);
	}

	[Fact]
	public async Task WhenNoDaemonListensAndNoneCanBeFound_ThenOneLineSaysHowToStartItAndTheExitCodeIs69()
	{
		CliResult result = await CliProcess.RunAsync(
			["get_symbol", "--solutionId", "s1", "--symbolName", "Calculator"],
			CliProcess.UnusedPort(),
			environment: new Dictionary<string, string>
			{
				["Roslynk__Cli__AutoStart"] = "true",
				["Roslynk__Cli__Daemon"] = Path.Combine(Path.GetTempPath(), $"no-such-roslynk-{Guid.NewGuid():N}"),
			});

		Assert.Equal(69, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		string line = Assert.Single(result.StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries));
		Assert.Contains("none was found to start", line, StringComparison.Ordinal);
	}

	[Fact]
	public async Task WhenTheDaemonAcceptsButDoesNotAnswerInTime_ThenOneLineSaysSoAndTheExitCodeIs69()
	{
		Daemon.Respond = _ =>
		{
			Thread.Sleep(TimeSpan.FromSeconds(4));
			return StubDaemon.Text("too late");
		};

		CliResult result = await CliProcess.RunAsync(
			["get_symbol", "--solutionId", "s1", "--symbolName", "Calculator"],
			Daemon.Port,
			environment: new Dictionary<string, string> { ["Roslynk__Cli__TimeoutSeconds"] = "1" });

		Assert.Equal(69, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		string line = Assert.Single(result.StandardError.Split('\n', StringSplitOptions.RemoveEmptyEntries));
		Assert.Contains("did not answer get_symbol: no answer within 1s", line, StringComparison.Ordinal);
	}

	[Fact]
	public async Task WhenARequiredOptionIsMissing_ThenNothingIsForwardedAndTheExitCodeIsOne()
	{
		CliResult result = await CliProcess.RunAsync(["get_symbol", "--solutionId", "s1"], Daemon.Port);

		Assert.Equal(1, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		Assert.Empty(Daemon.Calls);
	}
}
