using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Protocol;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// One name per concept on the published MCP surface (mrpmorris/Roslynk#121), and the prose held to it
/// (mrpmorris/Roslynk#120): a parameter-shaped name in the server instructions or a tool description must be
/// a parameter a tool publishes, a tool name, or a result field the output protocol defines. Ported from
/// upstream's host tests onto what <c>roslynk stdio</c> actually lists.
/// </summary>
public sealed class PublishedNamesTests : IClassFixture<McpSurfaceTests.Session>
{
	private readonly McpSurfaceTests.Session Fixture;

	public PublishedNamesTests(McpSurfaceTests.Session fixture)
	{
		Fixture = fixture;
	}

	[Fact]
	public void WhenEverySymbolToolIsPublished_ThenItNamesTheSymbolAsSymbolName()
	{
		// A caller that learned symbolName from get_symbol can reuse it everywhere.
		string[] symbolTools =
		[
			"get_symbol", "get_symbol_body", "get_members", "find_references", "find_reads", "find_writes",
			"get_callers", "get_callees", "find_implementations", "get_type_hierarchy", "rename_symbol",
			"rename_parameter", "change_signature",
		];

		foreach (string tool in symbolTools)
		{
			JsonElement properties = Fixture.Tool(tool).InputSchema.GetProperty("properties");
			Assert.True(properties.TryGetProperty("symbolName", out _), $"'{tool}' does not take 'symbolName'.");
			foreach (string stale in (string[])["typeName", "memberName", "methodName", "methodId"])
				Assert.False(properties.TryGetProperty(stale, out _), $"'{tool}' still publishes '{stale}'.");
		}
	}

	[Fact]
	public void WhenEveryFileToolIsPublished_ThenItNamesTheFileAsFilePath()
	{
		string[] fileTools =
		[
			"find_definition", "get_expression_info", "get_code_actions", "apply_code_fix", "extract_method",
			"remove_unused_usings", "get_diagnostics",
		];

		foreach (string tool in fileTools)
		{
			JsonElement properties = Fixture.Tool(tool).InputSchema.GetProperty("properties");
			Assert.True(properties.TryGetProperty("filePath", out _), $"'{tool}' does not take 'filePath'.");
			Assert.False(properties.TryGetProperty("documentPath", out _), $"'{tool}' still publishes 'documentPath'.");
		}
	}

	[Fact]
	public void WhenEveryPagingToolIsPublished_ThenItNamesItsPageAsStartItemAndMaxSize()
	{
		foreach (Tool tool in Fixture.Tools)
		{
			JsonElement properties = tool.InputSchema.GetProperty("properties");
			if (!properties.TryGetProperty("startItem", out _))
				continue;

			Assert.True(properties.TryGetProperty("maxSize", out _), $"'{tool.Name}' pages but publishes no 'maxSize'.");
			Assert.True(properties.TryGetProperty("countOnly", out _), $"'{tool.Name}' pages but publishes no 'countOnly'.");
		}
	}

	[Fact]
	public void WhenToolsArePublished_ThenNoToolPublishesMaxResults()
	{
		// maxResults merged into maxSize: one paging shape everywhere, with nothing dropped.
		foreach (Tool tool in Fixture.Tools)
			Assert.False(tool.InputSchema.GetProperty("properties").TryGetProperty("maxResults", out _), $"'{tool.Name}' still publishes 'maxResults'.");
	}

	[Fact]
	public void WhenTheServerInstructionsNameAParameter_ThenThatNameExistsOnAPublishedTool()
	{
		// The instructions once said "documentPath accepts .cs/.razor/.cshtml" while get_diagnostics takes
		// filePath. Every camelCase token in the instructions is held to a parameter some tool publishes, a
		// tool name, or a result field; anything else is a name a caller could send and have refused.
		HashSet<string> parameters = Fixture.Tools
			.SelectMany(tool => tool.InputSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name))
			.ToHashSet(StringComparer.Ordinal);

		Assert.Empty(Unexplained(Fixture.Client.ServerInstructions ?? "", parameters));
	}

	[Fact]
	public void WhenAToolDescriptionNamesAParameter_ThenThatParameterBelongsToTheToolOrIsAField()
	{
		// The same drift in the per-tool descriptions: a description may name its own parameters, another
		// tool ("pass actionId to apply_code_action") or a result header/leaf field.
		var unexplained = new List<string>();
		foreach (Tool tool in Fixture.Tools)
		{
			HashSet<string> own = tool.InputSchema.GetProperty("properties").EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
			unexplained.AddRange(Unexplained(tool.Description ?? "", own).Select(token => $"{tool.Name}:{token}"));
		}

		Assert.Empty(unexplained);
	}

	/// <summary>
	/// The camelCase tokens of <paramref name="text"/> that are not one of <paramref name="parameters"/>, a tool
	/// name or a result field. Lowercase words are prose and PascalCase words are type/symbol examples.
	/// </summary>
	private IEnumerable<string> Unexplained(string text, HashSet<string> parameters)
	{
		HashSet<string> toolNames = Fixture.Tools.Select(tool => tool.Name).ToHashSet(StringComparer.Ordinal);
		return Regex.Matches(text, @"\b[a-z][A-Za-z0-9]*\b")
			.Select(match => match.Value)
			.Where(token => Regex.IsMatch(token, "^[a-z]+[A-Z]"))
			.Where(token => !parameters.Contains(token) && !toolNames.Contains(token) && !ResultFields.Contains(token))
			.Distinct(StringComparer.Ordinal);
	}

	/// <summary>
	/// Result names the output protocol defines: headers and outline leaf fields prose may name without them
	/// being parameters. An explicit list, so a new result field is a deliberate addition.
	/// </summary>
	private static readonly HashSet<string> ResultFields = new(StringComparer.Ordinal)
	{
		// Output protocol headers, and the MCP result flag a failure sets
		"errorMessage", "fullOutputChars", "outputTruncated", "partialItem", "nextStartItem", "isError",
		// Resolution and outline leaves
		"resolvedSymbol", "resolvedMethod", "resolvedType", "fullName", "candidates", "symbolKind",
		"resolvedPath",
		"memberKind", "memberName", "typeKind", "typeName", "parameterTypes", "accessKind", "exprKind",
		"symbolLoc", "symbolPath", "flowState", "convertedType", "candidateReason",
		// Per-tool result headers and leaf fields
		"actionId", "diagnosticId", "documentVersion", "updatedCallSites", "unchangedRelated", "renamedMembers",
		"removedCount", "deadConditionals", "omittedParts", "loadDiagnostic", "loadDiagnostics",
		"loadDiagnosticsTruncated", "watchDiagnostic", "watchDiagnosticsTruncated", "documentCount", "projectPath", "solutionId", "startItem", "maxSize",
		"checkOnly", "baseVersions", "expectSnapshot",
		// extract_method emits symbolName as a header: the extracted member's usable signature.
		"symbolName",
	};
}
