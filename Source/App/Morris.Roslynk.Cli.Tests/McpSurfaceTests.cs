using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Morris.Roslynk.Cli.Tests.Infrastructure;
using Morris.Roslynk.Features.MultiQuery;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// What an agent sees from <c>roslynk stdio</c> (Nuru McpHost over the app's catalog): one tool per operation
/// with its name, description, title, required members and annotations; real JSON schema for the object and
/// array members; the shared server instructions; and the error and binding behaviour. These replace the old
/// pinned schema tests: they check names, descriptions, required members, annotations and behaviour rather
/// than bytes.
/// </summary>
public sealed class McpSurfaceTests : IClassFixture<McpSurfaceTests.Session>
{
	private readonly Session Fixture;

	public McpSurfaceTests(Session fixture)
	{
		Fixture = fixture;
	}

	[Fact]
	public void WhenToolsAreListed_ThenThereIsOneToolPerOperationAndNoStdio()
	{
		Assert.Equal(
			Operations.All.Select(operation => operation.Name),
			Fixture.Tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
	}

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAToolIsListed_ThenItCarriesTheOperationsDescriptionAndTitle(string name)
	{
		OperationContract operation = Operations.Named(name);
		Tool tool = Fixture.Tool(name);

		Assert.Equal(operation.Description, tool.Description);
		Assert.Equal(operation.Title, tool.Title);
		Assert.Equal(operation.Title, tool.Annotations?.Title);
	}

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAToolIsListed_ThenItsAnnotationsFollowItsKindAndItIsClosedWorld(string name)
	{
		OperationContract operation = Operations.Named(name);
		ToolAnnotations annotations = Fixture.Tool(name).Annotations ?? throw new Xunit.Sdk.XunitException($"{name} has no annotations.");

		Assert.False(annotations.OpenWorldHint);
		Assert.Equal(operation.IsCommand, annotations.DestructiveHint);
		Assert.Equal(!operation.IsCommand, annotations.ReadOnlyHint);
	}

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAToolIsListed_ThenItsPropertiesAndRequiredSetAreTheRequestMembers(string name)
	{
		OperationContract operation = Operations.Named(name);
		JsonElement schema = Fixture.Tool(name).InputSchema;

		string[] properties = schema.GetProperty("properties").EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
		string[] required = schema.TryGetProperty("required", out JsonElement list)
			? list.EnumerateArray().Select(item => item.GetString()!).Order(StringComparer.Ordinal).ToArray()
			: [];

		Assert.Equal(operation.Members.Select(member => member.Name).Order(StringComparer.Ordinal), properties);
		Assert.Equal(operation.RequiredMembers.Order(StringComparer.Ordinal), required);
		Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
	}

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAToolTakesSolutionId_ThenItIsARequiredString(string name)
	{
		JsonElement properties = Fixture.Tool(name).InputSchema.GetProperty("properties");
		if (!properties.TryGetProperty("solutionId", out JsonElement solutionId))
			return;

		Assert.Equal("string", solutionId.GetProperty("type").GetString());
		Assert.Contains("solutionId", Operations.Named(name).RequiredMembers);
	}

	[Fact]
	public void WhenAMemberHasADefault_ThenTheSchemaAndTheDescriptionBothCarryIt()
	{
		JsonElement includeAnalyzers = Fixture.Tool("get_diagnostics").InputSchema.GetProperty("properties").GetProperty("includeAnalyzers");

		Assert.True(includeAnalyzers.GetProperty("default").GetBoolean());
		Assert.EndsWith("Default: true.", includeAnalyzers.GetProperty("description").GetString());
	}

	[Fact]
	public void WhenMultiQueryIsListed_ThenOperationsIsAnArrayOfToolEnumAndArgumentsObjects()
	{
		JsonElement operations = Fixture.Tool("multi_query").InputSchema.GetProperty("properties").GetProperty("operations");
		JsonElement item = operations.GetProperty("items");

		Assert.Equal("array", operations.GetProperty("type").GetString());
		Assert.Equal("object", item.GetProperty("type").GetString());
		Assert.Equal(["tool"], item.GetProperty("required").EnumerateArray().Select(entry => entry.GetString()));
		Assert.Equal(
			Enum.GetNames<MultiQueryOp>(),
			item.GetProperty("properties").GetProperty("tool").GetProperty("enum").EnumerateArray().Select(entry => entry.GetString()));
		Assert.Contains("object", item.GetProperty("properties").GetProperty("arguments").GetProperty("type").EnumerateArray().Select(entry => entry.GetString()));
	}

	[Fact]
	public void WhenApplyPatchIsListed_ThenBaseVersionsIsAnOptionalArrayOfPathVersionPairs()
	{
		JsonElement baseVersions = Fixture.Tool("apply_patch").InputSchema.GetProperty("properties").GetProperty("baseVersions");
		JsonElement item = baseVersions.GetProperty("items");

		Assert.Contains("array", baseVersions.GetProperty("type").EnumerateArray().Select(entry => entry.GetString()));
		Assert.Equal("object", item.GetProperty("type").GetString());
		Assert.Equal(["path", "version"], item.GetProperty("required").EnumerateArray().Select(entry => entry.GetString()).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void WhenGetDiagnosticsIsListed_ThenIdsIsAnArrayOfStrings()
	{
		JsonElement ids = Fixture.Tool("get_diagnostics").InputSchema.GetProperty("properties").GetProperty("ids");

		Assert.Equal("string", ids.GetProperty("items").GetProperty("type").GetString());
	}

	[Fact]
	public void WhenTheServerInitializes_ThenItsInstructionsCarryTheSharedGuidance()
	{
		string instructions = Fixture.Client.ServerInstructions ?? "";

		Assert.Contains("## Lifecycle", instructions);
		Assert.Contains("## Output protocol", instructions);
		Assert.Contains("## Failures and freshness", instructions);
		Assert.Contains("isError", instructions);
		Assert.Contains("## Batching", instructions);
		Assert.Equal("roslynk", Fixture.Client.ServerInfo.Name);
	}

	[Fact]
	public void WhenTheImpactRecipeIsDocumented_ThenEveryArgumentNameIsARealParameterOfItsTool()
	{
		// The impact-analysis batch from the roslynk skill: multi_query rejects unknown argument keys, so a
		// recipe argument that drifts from a tool's parameters is a broken recipe.
		(string Tool, string Argument)[] recipe =
		[
			("get_symbol", "symbolName"),
			("find_references", "symbolName"),
			("get_callers", "methodName"),
			("get_callees", "memberName"),
			("find_implementations", "symbolName"),
			("get_type_hierarchy", "typeName"),
		];

		foreach ((string tool, string argument) in recipe)
			Assert.True(Fixture.Tool(tool).InputSchema.GetProperty("properties").TryGetProperty(argument, out _), $"'{argument}' is not a parameter of '{tool}'.");
	}

	[Fact]
	public async Task WhenACallSucceeds_ThenItsTextIsTheOutlineTheCliPrints()
	{
		(string text, bool isError) = await Fixture.Client.CallAsync("get_symbol", new() { ["solutionId"] = Fixture.SolutionId, ["symbolName"] = ServedSolution.Calculator });
		CliResult cli = await Fixture.RunAsync(["get_symbol", "--solutionId", Fixture.SolutionId, "--symbolName", ServedSolution.Calculator]);

		Assert.False(isError, text);
		Assert.StartsWith("project=SimpleLibrary\n", text);
		Assert.Equal(cli.StandardOutput.TrimEnd('\n'), text);
	}

	[Fact]
	public async Task WhenAToolReportsAProblem_ThenTheResultIsAnErrorCarryingTheErrorOutline()
	{
		(string text, bool isError) = await Fixture.Client.CallAsync("get_symbol", new() { ["solutionId"] = Fixture.SolutionId, ["symbolName"] = "NoSuchSymbol" });

		Assert.True(isError);
		Assert.StartsWith("error=NotFound\nerrorMessage=", text);
	}

	[Fact]
	public async Task WhenAnArgumentIsUnknown_ThenTheResultIsABindingError()
	{
		(string text, bool isError) = await Fixture.Client.CallAsync("get_symbol", new() { ["solutionId"] = Fixture.SolutionId, ["symbolName"] = "x", ["bogus"] = 1 });

		Assert.True(isError);
		Assert.Contains("bogus", text);
	}

	[Fact]
	public async Task WhenAMultiQueryOperationNamesAToolOutsideTheEnum_ThenTheCallIsRejected()
	{
		(string text, bool isError) = await Fixture.Client.CallAsync("multi_query", new()
		{
			["solutionId"] = Fixture.SolutionId,
			["operations"] = new[] { new Dictionary<string, object> { ["tool"] = "rename_symbol" } },
		});

		Assert.True(isError, text);
	}

	[Fact]
	public async Task WhenAMultiQueryBatchRuns_ThenEveryOperationIsDelivered()
	{
		(string text, bool isError) = await Fixture.Client.CallAsync("multi_query", new()
		{
			["solutionId"] = Fixture.SolutionId,
			["operations"] = new object[]
			{
				new Dictionary<string, object> { ["tool"] = "get_symbol", ["arguments"] = new Dictionary<string, object> { ["symbolName"] = ServedSolution.Calculator } },
				new Dictionary<string, object> { ["tool"] = "get_members", ["arguments"] = new Dictionary<string, object> { ["typeName"] = ServedSolution.Calculator } },
			},
		});

		Assert.False(isError, text);
		Assert.DoesNotContain("retry=", text);
		Assert.Contains("tool=get_symbol", text);
		Assert.Contains("tool=get_members", text);
	}

	/// <summary>The served Simple fixture plus one MCP session and its tool list.</summary>
	public sealed class Session : ServedSolution
	{
		public McpClient Client { get; private set; } = null!;

		public IReadOnlyList<Tool> Tools { get; private set; } = [];

		public Tool Tool(string name) => Tools.Single(tool => tool.Name == name);

		public override async Task InitializeAsync()
		{
			await base.InitializeAsync();
			Client = await ConnectAsync();
			Tools = (await Client.ListToolsAsync()).Select(tool => tool.ProtocolTool).ToArray();
		}

		public override async Task DisposeAsync()
		{
			if (Client is not null)
				await Client.DisposeAsync();
			await base.DisposeAsync();
		}
	}
}
