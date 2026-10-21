using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// A forwarded call of any operation route, or <c>--capabilities</c>, never loads Roslyn or MSBuild in the CLI process,
/// although the roslynk package carries them for the server it starts. The engine assembly itself may load:
/// it declares the [Operation] contracts the routes bind, and only touching an engine type past them would
/// pull Roslyn in. The test assembly doubles as a .NET startup hook (<see cref="StartupHook"/>) that writes
/// the assemblies loaded at exit.
/// </summary>
public class ProcessIsolationTests : IClassFixture<ServedSolution>
{
	private static bool IsForbidden(string name) =>
		name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal)
		|| name.StartsWith("Microsoft.Build", StringComparison.Ordinal)
		|| name.StartsWith("ModelContextProtocol", StringComparison.Ordinal)
		|| name.StartsWith("OpenTelemetry", StringComparison.Ordinal);

	private readonly ServedSolution Served;

	public ProcessIsolationTests(ServedSolution served)
	{
		Served = served;
	}

	public static TheoryData<string> Routes() => ["--capabilities", .. Operations.All.Select(operation => operation.Name)];

	[Theory]
	[MemberData(nameof(Routes))]
	public async Task WhenTheCliRuns_ThenNoRoslynOrMSBuildAssemblyIsLoaded(string route)
	{
		string list = Path.Combine(Path.GetTempPath(), $"roslynk-assemblies-{Guid.NewGuid():N}.txt");
		string[] argv = route == "--capabilities" ? [route] : [route, "--json-args", ArgumentsOf(Operations.Named(route)).ToJsonString()];
		try
		{
			CliResult result = await Served.RunAsync(argv, new Dictionary<string, string?>
			{
				["DOTNET_STARTUP_HOOKS"] = typeof(StartupHook).Assembly.Location,
				[StartupHook.ListVariable] = list,
			});

			// An operation call binds and is forwarded: the server's outcome may be a problem (exit 2 to 10), but
			// never a usage error (1) or an unavailable server (69).
			if (route == "--capabilities")
				Assert.True(result.ExitCode == 0, result.StandardError);
			else
				Assert.True(result.ExitCode is 0 or (>= 2 and <= 10), $"exit {result.ExitCode}: {result.StandardError}");
			string[] loaded = await File.ReadAllLinesAsync(list);
			Assert.Contains("roslynk", loaded);
			Assert.Contains("TimeWarp.Nuru", loaded);
			if (route != "--capabilities")
				Assert.Contains("TimeWarp.Nuru.Daemon", loaded);
			Assert.DoesNotContain(loaded, IsForbidden);
		}
		finally
		{
			File.Delete(list);
		}
	}

	/// <summary>
	/// The required members of an operation, against the served fixture, with writes previewed only. Values other
	/// than the solution are placeholders: the call need not succeed, only bind and be forwarded.
	/// </summary>
	private JsonObject ArgumentsOf(OperationContract operation)
	{
		JsonObject arguments = new();
		foreach (PropertyInfo member in operation.Members.Where(member => operation.RequiredMembers.Contains(member.Name)))
			arguments[member.Name] = member.Name switch
			{
				"solutionId" => Served.SolutionId,
				"solutionPath" => Served.SolutionPath,
				_ => SampleOf(member),
			};
		if (operation.Members.Any(member => member.Name == "checkOnly"))
			arguments["checkOnly"] = true;
		return arguments;
	}

	private static JsonNode SampleOf(PropertyInfo member)
	{
		Type type = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;
		if (type == typeof(string))
			return JsonValue.Create("x")!;
		if (type == typeof(int))
			return JsonValue.Create(1)!;
		if (type == typeof(bool))
			return JsonValue.Create(false)!;
		if (type.Name.StartsWith("IReadOnlyList", StringComparison.Ordinal) && type.GetGenericArguments()[0].Name == "MultiQueryOperation")
			return new JsonArray(new JsonObject { ["tool"] = "get_symbol", ["arguments"] = new JsonObject { ["symbolName"] = ServedSolution.Calculator } });
		throw new Xunit.Sdk.XunitException($"No sample for required {member.DeclaringType}.{member.Name} ({member.PropertyType}).");
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
