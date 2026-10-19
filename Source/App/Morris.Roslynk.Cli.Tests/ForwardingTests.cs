using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 2: every route resolves a handler and forwards the bound request as the <c>tools/call</c>
/// an MCP client would make. The arguments are built from the published MCP schema, so a route whose
/// option, value or JSON shape drifted from the tool would fail here.
/// </summary>
public class ForwardingTests : IClassFixture<StubDaemon>
{
	private readonly StubDaemon Daemon;

	public ForwardingTests(StubDaemon daemon)
	{
		Daemon = daemon;
		Daemon.Reset();
	}

	/// <summary>Sample values for the two members that only <c>--json-args</c> can carry.</summary>
	private static readonly Dictionary<(string Tool, string Parameter), string> JsonSamples = new()
	{
		[("multi_query", "operations")] =
			"""[{"tool":"get_symbol","arguments":{"symbolName":"Calculator"}},{"tool":"find_references","arguments":{"symbolName":"Calculator.Add","maxResults":3}}]""",
		[("apply_patch", "baseVersions")] =
			"""[{"path":"SimpleLibrary/Calculator.cs","version":"0123456789abcdef"}]""",
	};

	[Theory]
	[MemberData(nameof(PublishedTools.ToolNames), MemberType = typeof(PublishedTools))]
	public async Task WhenEveryParameterIsPassed_ThenTheDaemonReceivesTheSameToolAndArguments(string toolName)
	{
		(List<string> argv, JsonObject expected) = Invocation(toolName, requiredOnly: false);

		CallToolRequestParams call = await ForwardAsync(argv);

		Assert.Equal(toolName, call.Name);
		AssertArguments(toolName, expected, call);
	}

	[Theory]
	[MemberData(nameof(PublishedTools.ToolNames), MemberType = typeof(PublishedTools))]
	public async Task WhenOnlyRequiredParametersArePassed_ThenDefaultsAreLeftToTheDaemon(string toolName)
	{
		(List<string> argv, JsonObject expected) = Invocation(toolName, requiredOnly: true);

		CallToolRequestParams call = await ForwardAsync(argv);

		Assert.Equal(toolName, call.Name);
		AssertArguments(toolName, expected, call);
		Assert.Equal(PublishedTools.Required(PublishedTools.Named(toolName)).Order(StringComparer.Ordinal), (call.Arguments?.Keys ?? []).Order(StringComparer.Ordinal));
	}

	[Fact]
	public async Task WhenMultiQueryOperationsComeFromStandardInput_ThenTheyArriveAsTheSameJson()
	{
		string operations = JsonSamples[("multi_query", "operations")];

		CallToolRequestParams call = await ForwardAsync(
			["multi_query", "--json-args", "-"],
			standardInput: $$"""{"solutionId":"s1","operations":{{operations}},"expectSnapshot":"g1"}""");

		Assert.Equal("multi_query", call.Name);
		Assert.True(JsonNode.DeepEquals(JsonNode.Parse(operations), JsonNode.Parse(call.Arguments!["operations"].GetRawText())), call.Arguments!["operations"].GetRawText());
		Assert.Equal("g1", call.Arguments!["expectSnapshot"].GetString());
	}

	[Fact]
	public async Task WhenApplyPatchBaseVersionsComeFromAFile_ThenTheyArriveAsTheSameJson()
	{
		string baseVersions = JsonSamples[("apply_patch", "baseVersions")];
		string file = Path.Combine(Path.GetTempPath(), $"roslynk-{Guid.NewGuid():N}.json");
		await File.WriteAllTextAsync(file, $$"""{"solutionId":"s1","patch":"--- a/x\n+++ b/x\n","baseVersions":{{baseVersions}}}""");
		try
		{
			CallToolRequestParams call = await ForwardAsync(["apply_patch", "--checkOnly", "--json-args", "@" + file]);

			Assert.Equal("apply_patch", call.Name);
			Assert.True(JsonNode.DeepEquals(JsonNode.Parse(baseVersions), JsonNode.Parse(call.Arguments!["baseVersions"].GetRawText())));
			Assert.Equal("--- a/x\n+++ b/x\n", call.Arguments!["patch"].GetString());
			Assert.True(call.Arguments!["checkOnly"].GetBoolean());
		}
		finally
		{
			File.Delete(file);
		}
	}

	private async Task<CallToolRequestParams> ForwardAsync(IEnumerable<string> argv, string? standardInput = null)
	{
		Daemon.Calls.Clear();
		CliResult result = await CliProcess.RunAsync(argv, Daemon.Port, standardInput);

		Assert.True(result.ExitCode == 0, $"exit {result.ExitCode}: {result.StandardError}");
		Assert.Equal("ok", result.StandardOutput);
		return Assert.Single(Daemon.Calls);
	}

	/// <summary>
	/// The argv for one route and the MCP arguments it must produce, built from the published schema: a
	/// non-default value for every parameter (or only the required ones), each spelled the way the CLI
	/// catalog says that option takes it.
	/// </summary>
	private static (List<string> Argv, JsonObject Expected) Invocation(string toolName, bool requiredOnly)
	{
		Tool tool = PublishedTools.Named(toolName);
		IReadOnlySet<string> required = PublishedTools.Required(tool);
		Dictionary<string, JsonElement> options = PublishedTools.Endpoint(toolName).GetProperty("options").EnumerateArray()
			.ToDictionary(option => option.GetProperty("name").GetString()!, StringComparer.Ordinal);

		var argv = new List<string> { toolName };
		var expected = new JsonObject();
		var jsonArgs = new JsonObject();

		foreach ((string name, JsonElement parameter) in PublishedTools.Properties(tool))
		{
			if (requiredOnly && !required.Contains(name))
				continue;

			JsonElement option = options[name];
			string type = option.GetProperty("type").GetString()!;
			string schemaType = SchemaType(parameter);

			if (type.StartsWith("json", StringComparison.Ordinal))
			{
				JsonNode sample = JsonNode.Parse(JsonSamples[(toolName, name)])!;
				jsonArgs[name] = sample.DeepClone();
				expected[name] = sample;
			}
			else if (option.GetProperty("isRepeated").GetBoolean())
			{
				argv.AddRange([$"--{name}", $"{name}-1", $"--{name}", $"{name}-2"]);
				expected[name] = new JsonArray($"{name}-1", $"{name}-2");
				Assert.Equal("array", schemaType);
			}
			else if (type == "bool")
			{
				bool flag = option.GetProperty("isFlag").GetBoolean();
				bool value = flag || option.GetProperty("defaultValue").GetString() != "true";
				argv.AddRange(flag ? [$"--{name}"] : [$"--{name}", value ? "true" : "false"]);
				expected[name] = value;
				Assert.Equal("boolean", schemaType);
			}
			else if (type is "int" or "int?")
			{
				argv.AddRange([$"--{name}", "7"]);
				expected[name] = 7;
				Assert.Equal("integer", schemaType);
			}
			else
			{
				Assert.Equal("string", type);
				argv.AddRange([$"--{name}", $"value of {name}"]);
				expected[name] = $"value of {name}";
				Assert.Equal("string", schemaType);
			}
		}

		if (jsonArgs.Count > 0)
			argv.AddRange(["--json-args", jsonArgs.ToJsonString()]);

		return (argv, expected);
	}

	private static void AssertArguments(string toolName, JsonObject expected, CallToolRequestParams call)
	{
		var actual = new JsonObject();
		foreach ((string key, JsonElement value) in call.Arguments ?? new Dictionary<string, JsonElement>())
			actual[key] = JsonNode.Parse(value.GetRawText());

		Assert.True(JsonNode.DeepEquals(expected, actual), $"{toolName}\nexpected: {expected.ToJsonString()}\nactual:   {actual.ToJsonString()}");
	}

	/// <summary>The schema's JSON type, ignoring the "null" a nullable parameter adds.</summary>
	private static string SchemaType(JsonElement parameter)
	{
		JsonElement type = parameter.GetProperty("type");
		return type.ValueKind == JsonValueKind.Array
			? type.EnumerateArray().Select(entry => entry.GetString()!).Single(entry => entry != "null")
			: type.GetString()!;
	}
}
