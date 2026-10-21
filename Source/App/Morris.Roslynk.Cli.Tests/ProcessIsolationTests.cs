using System.Text.Json;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// A forwarded operation call or <c>--capabilities</c> never loads Roslyn, MSBuild or the engine in the CLI
/// process, although the roslynk package carries them for the server it starts. The test assembly doubles as
/// a .NET startup hook (<see cref="StartupHook"/>) that writes the assemblies loaded at exit.
/// </summary>
public class ProcessIsolationTests : IClassFixture<ServedSolution>
{
	private static bool IsForbidden(string name) =>
		name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
		|| name.StartsWith("Microsoft.Build", StringComparison.Ordinal)
		|| name == "Morris.Roslynk";

	private readonly ServedSolution Served;

	public ProcessIsolationTests(ServedSolution served)
	{
		Served = served;
	}

	[Theory]
	[InlineData("--capabilities")]
	[InlineData("get_symbol")]
	public async Task WhenTheCliRuns_ThenNoRoslynOrEngineAssemblyIsLoaded(string route)
	{
		string list = Path.Combine(Path.GetTempPath(), $"roslynk-assemblies-{Guid.NewGuid():N}.txt");
		string[] argv = route == "get_symbol" ? ["get_symbol", "--solutionId", Served.SolutionId, "--symbolName", ServedSolution.Calculator] : [route];
		try
		{
			CliResult result = await Served.RunAsync(argv, new Dictionary<string, string?>
			{
				["DOTNET_STARTUP_HOOKS"] = typeof(StartupHook).Assembly.Location,
				[StartupHook.ListVariable] = list,
			});

			Assert.True(result.ExitCode == 0, result.StandardError);
			string[] loaded = await File.ReadAllLinesAsync(list);
			Assert.Contains("roslynk", loaded);
			Assert.Contains("TimeWarp.Nuru", loaded);
			if (route == "get_symbol")
				Assert.Contains("TimeWarp.Nuru.Daemon", loaded);
			Assert.DoesNotContain(loaded, IsForbidden);
		}
		finally
		{
			File.Delete(list);
		}
	}

	[Fact]
	public void WhenTheCommandIsBuilt_ThenItCarriesTheEngineAndNoAspNetHost()
	{
		string depsFile = Path.ChangeExtension(CliProcess.CliAssembly, ".deps.json");
		using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(depsFile));

		string[] libraries = deps.RootElement.GetProperty("libraries").EnumerateObject()
			.Select(library => library.Name.Split('/')[0])
			.ToArray();

		// One package, one command: the isolation above is a load-time property, not a packaging one.
		Assert.Contains("Morris.Roslynk", libraries);
		Assert.Contains("TimeWarp.Nuru.Daemon", libraries);
		Assert.Contains("TimeWarp.Nuru.McpHost", libraries);
		Assert.DoesNotContain("Morris.Roslynk.Mcp", libraries);
		Assert.DoesNotContain(libraries, library => library.StartsWith("ModelContextProtocol.AspNetCore", StringComparison.Ordinal));

		string runtimeConfig = File.ReadAllText(Path.ChangeExtension(CliProcess.CliAssembly, ".runtimeconfig.json"));
		Assert.DoesNotContain("Microsoft.AspNetCore.App", runtimeConfig);
	}
}
