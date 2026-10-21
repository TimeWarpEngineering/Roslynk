using System.Text.Json;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// CLI calls forwarded to the served daemon: the success outline on stdout, the <c>error=</c> outline on
/// stderr with the documented exit code, Nuru's own usage and binding errors as exit 1 (a binding error with
/// the <c>error=Invalid</c> outline), the caller's directory for
/// relative paths, and <c>--no-daemon</c> running the same handler in the CLI process.
/// </summary>
public class ServedCliTests : IClassFixture<ServedSolution>
{
	private readonly ServedSolution Served;

	public ServedCliTests(ServedSolution served)
	{
		Served = served;
	}

	[Fact]
	public async Task WhenACallSucceeds_ThenStdoutIsTheOutlineAndTheExitCodeIsZero()
	{
		CliResult result = await Served.RunAsync(["get_symbol", "--solutionId", Served.SolutionId, "--symbolName", ServedSolution.Calculator]);

		Assert.Equal(0, result.ExitCode);
		Assert.Equal("", result.StandardError);
		Assert.StartsWith("project=SimpleLibrary\npath=SimpleLibrary/Calculator.cs\nloc=", result.StandardOutput);
		Assert.Contains("\n\npublic class Calculator", result.StandardOutput);
	}

	[Fact]
	public async Task WhenAToolReportsNotFound_ThenStderrIsTheErrorOutlineAndTheExitCodeIsThree()
	{
		CliResult result = await Served.RunAsync(["get_symbol", "--solutionId", Served.SolutionId, "--symbolName", "NoSuchSymbol"]);

		Assert.Equal(3, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		Assert.StartsWith("error=NotFound\nerrorMessage=No symbol matched 'NoSuchSymbol'.", result.StandardError);
	}

	[Fact]
	public async Task WhenAToolReportsInvalid_ThenTheExitCodeIsTwo()
	{
		CliResult result = await Served.RunAsync(["multi_query", "--json-args", $$"""{"solutionId":"{{Served.SolutionId}}","operations":[]}"""]);

		Assert.Equal(2, result.ExitCode);
		Assert.StartsWith("error=Invalid\n", result.StandardError);
	}

	[Fact]
	public async Task WhenARequiredOptionIsMissing_ThenNuruReportsAUsageErrorWithExitOne()
	{
		CliResult result = await Served.RunAsync(["get_symbol", "--solutionId", Served.SolutionId]);

		Assert.Equal(1, result.ExitCode);
		Assert.DoesNotContain("error=", result.StandardError);
	}

	[Theory]
	[InlineData("value")]
	[InlineData("json")]
	public async Task WhenAnArgumentDoesNotBind_ThenStderrIsTheInvalidOutlineWithExitOne(string kind)
	{
		string[] argv = kind == "value"
			? ["get_members", "--solutionId", Served.SolutionId, "--typeName", ServedSolution.Calculator, "--startItem", "first"]
			: ["get_symbol", "--json-args", $$"""{"solutionId":"{{Served.SolutionId}}","symbolName":"x","bogus":1}"""];
		CliResult result = await Served.RunAsync(argv);

		Assert.Equal(1, result.ExitCode);
		Assert.Equal("", result.StandardOutput);
		Assert.StartsWith("error=Invalid\nerrorMessage=", result.StandardError);
	}

	[Fact]
	public async Task WhenJsonArgumentsCarryABatch_ThenTheOperationsRunInTheServer()
	{
		string batch = JsonSerializer.Serialize(new
		{
			solutionId = Served.SolutionId,
			operations = new object[]
			{
				new { tool = "get_symbol", arguments = new { symbolName = ServedSolution.Calculator } },
				new { tool = "search_symbols", arguments = new { query = "Calc" } },
			},
		});
		CliResult result = await Served.RunAsync(["multi_query", "--json-args", batch]);

		Assert.True(result.ExitCode == 0, result.StandardError);
		Assert.Contains("tool=get_symbol", result.StandardOutput);
		Assert.Contains("tool=search_symbols", result.StandardOutput);
		Assert.DoesNotContain("retry=", result.StandardOutput);
	}

	[Fact]
	public async Task WhenASolutionPathIsRelative_ThenItResolvesAgainstTheCallersDirectory()
	{
		CliResult result = await Served.RunAsync(["open_solution", "--solutionPath", "SimpleSolution.slnx"], workingDirectory: Served.SolutionDirectory);

		Assert.True(result.ExitCode == 0, result.StandardError);
		Assert.StartsWith($"solutionId={Served.SolutionId}\n", result.StandardOutput);
	}

	[Fact]
	public async Task WhenASolutionIdIsRelative_ThenItResolvesAgainstTheCallersDirectory()
	{
		CliResult result = await Served.RunAsync(["get_symbol", "--solutionId", "SimpleSolution.slnx", "--symbolName", ServedSolution.Calculator], workingDirectory: Served.SolutionDirectory);

		Assert.True(result.ExitCode == 0, result.StandardError);
		Assert.StartsWith("project=SimpleLibrary\n", result.StandardOutput);
	}

	[Fact]
	public async Task WhenACallOptsOutOfForwarding_ThenTheSameHandlerRunsInTheCliProcess()
	{
		// A fresh engine in this process: no solution is loaded there, unlike in the server.
		CliResult local = await Served.RunAsync(["--no-daemon", "get_solution_status"]);
		CliResult served = await Served.RunAsync(["get_solution_status"]);

		Assert.True(local.ExitCode == 0, local.StandardError);
		Assert.DoesNotContain(Served.SolutionId, local.StandardOutput);
		Assert.Contains($"{Served.SolutionId},Ready", served.StandardOutput);
	}
}
