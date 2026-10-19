using System.Text.Json;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 5: the CLI process never loads Roslyn, MSBuild or the engine. The test assembly doubles as
/// a .NET startup hook (<see cref="StartupHook"/>) that writes the assemblies loaded at exit.
/// </summary>
public class ProcessIsolationTests : IClassFixture<StubDaemon>
{
	private static bool IsForbidden(string name) =>
		name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
		|| name.StartsWith("Microsoft.Build", StringComparison.Ordinal)
		|| name is "Morris.Roslynk" or "Morris.Roslynk.Mcp";

	private readonly StubDaemon Daemon;

	public ProcessIsolationTests(StubDaemon daemon)
	{
		Daemon = daemon;
		Daemon.Reset();
	}

	[Theory]
	[InlineData("--capabilities")]
	[InlineData("get_symbol")]
	public async Task WhenTheCliRuns_ThenNoRoslynOrEngineAssemblyIsLoaded(string route)
	{
		string list = Path.Combine(Path.GetTempPath(), $"roslynk-cli-assemblies-{Guid.NewGuid():N}.txt");
		string[] argv = route == "get_symbol" ? ["get_symbol", "--solutionId", "s1", "--symbolName", "Calculator"] : [route];
		try
		{
			CliResult result = await CliProcess.RunAsync(argv, Daemon.Port, environment: new Dictionary<string, string>
			{
				["DOTNET_STARTUP_HOOKS"] = typeof(StartupHook).Assembly.Location,
				[StartupHook.ListVariable] = list,
			});

			Assert.True(result.ExitCode == 0, result.StandardError);
			string[] loaded = await File.ReadAllLinesAsync(list);
			Assert.Contains("roslynk-cli", loaded);
			Assert.Contains("TimeWarp.Nuru", loaded);
			if (route == "get_symbol")
				Assert.Contains("ModelContextProtocol.Core", loaded);
			Assert.DoesNotContain(loaded, IsForbidden);
		}
		finally
		{
			File.Delete(list);
		}
	}

	[Fact]
	public void WhenTheCliIsBuilt_ThenItsDependenciesCarryNoRoslynOrEngineAssembly()
	{
		string depsFile = Path.ChangeExtension(CliProcess.CliAssembly, ".deps.json");
		using JsonDocument deps = JsonDocument.Parse(File.ReadAllText(depsFile));

		string[] libraries = deps.RootElement.GetProperty("libraries").EnumerateObject().Select(library => library.Name).ToArray();

		Assert.DoesNotContain(libraries, library => IsForbidden(library.Split('/')[0]));
	}
}
