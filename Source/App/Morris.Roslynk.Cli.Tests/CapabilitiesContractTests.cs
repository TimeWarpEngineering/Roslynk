using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Protocol;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// Requirement 1: the CLI catalog is the MCP surface. Same 29 names, option names equal to parameter
/// names, the same tool and parameter descriptions, defaults and required set, checked against the schema
/// the daemon publishes.
/// </summary>
public partial class CapabilitiesContractTests
{
	/// <summary>The routes of the roslynk command that run the host in-process rather than an operation.</summary>
	private static readonly string[] HostRoutes = ["", "daemon {*settings}", "stdio"];

	/// <summary>
	/// The group each operation declares on its <c>[Operation]</c>, which Nuru reports as the route's
	/// <c>groupPath</c>. The keys are also the 29 operation names, which the groups must not change.
	/// </summary>
	private static readonly Dictionary<string, string> ExpectedGroups = new(StringComparer.Ordinal)
	{
		["get_symbol"] = "symbols",
		["get_members"] = "symbols",
		["get_symbol_body"] = "symbols",
		["find_definition"] = "symbols",
		["search_symbols"] = "symbols",
		["get_expression_info"] = "symbols",
		["get_type_hierarchy"] = "symbols",
		["find_implementations"] = "symbols",
		["find_references"] = "references",
		["get_callers"] = "references",
		["get_callees"] = "references",
		["find_reads"] = "references",
		["find_writes"] = "references",
		["find_dead_code"] = "references",
		["find_dead_conditionals"] = "references",
		["open_solution"] = "solution",
		["get_solution_status"] = "solution",
		["reload_solution"] = "solution",
		["get_diagnostics"] = "solution",
		["get_code_actions"] = "solution",
		["apply_code_action"] = "edit",
		["apply_code_fix"] = "edit",
		["extract_method"] = "edit",
		["change_signature"] = "edit",
		["rename_symbol"] = "edit",
		["rename_parameter"] = "edit",
		["remove_unused_usings"] = "edit",
		["apply_patch"] = "edit",
		["multi_query"] = "batch",
	};

	public static TheoryData<string> Groups() => ["symbols", "references", "solution", "edit", "batch"];

	[Fact]
	public void WhenCapabilitiesAreListed_ThenThereIsOneEndpointPerPublishedToolBesideTheHostRoutes()
	{
		string[] patterns = PublishedTools.Capabilities.GetProperty("endpoints").EnumerateArray()
			.Select(endpoint => endpoint.GetProperty("pattern").GetString()!)
			.ToArray();
		string[] tools = PublishedTools.Tools.Select(tool => tool.Name).Order(StringComparer.Ordinal).ToArray();

		Assert.Equal(29, tools.Length);
		Assert.Equal(tools, patterns.Except(HostRoutes).Order(StringComparer.Ordinal));
		Assert.Equal(HostRoutes, patterns.Intersect(HostRoutes).Order(StringComparer.Ordinal));
	}

	[Fact]
	public void WhenCapabilitiesAreListed_ThenTheOperationNamesAreTheGroupedOnes()
	{
		Assert.Equal(
			ExpectedGroups.Keys.Order(StringComparer.Ordinal),
			PublishedTools.Tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
	}

	[Theory]
	[MemberData(nameof(PublishedTools.ToolNames), MemberType = typeof(PublishedTools))]
	public void WhenAnEndpointIsListed_ThenItsGroupPathIsItsOneGroup(string toolName)
	{
		string[] groupPath = PublishedTools.Endpoint(toolName).GetProperty("groupPath").EnumerateArray()
			.Select(segment => segment.GetString()!)
			.ToArray();

		Assert.Equal([ExpectedGroups[toolName]], groupPath);
	}

	[Fact]
	public void WhenCapabilitiesAreListed_ThenTheHostRoutesAreUngrouped()
	{
		foreach (string route in HostRoutes)
			Assert.Empty(PublishedTools.Endpoint(route).GetProperty("groupPath").EnumerateArray());
	}

	[Theory]
	[MemberData(nameof(Groups))]
	public async Task WhenCapabilitiesAreFilteredByGroup_ThenOnlyThatGroupsRoutesAreListed(string group)
	{
		CliResult result = await CliProcess.RunAsync(["--capabilities", "--group-filter", group], CliProcess.UnusedPort());

		Assert.Equal(0, result.ExitCode);
		using JsonDocument capabilities = JsonDocument.Parse(result.StandardOutput);
		string[] patterns = capabilities.RootElement.GetProperty("endpoints").EnumerateArray()
			.Select(endpoint => endpoint.GetProperty("pattern").GetString()!)
			.Order(StringComparer.Ordinal)
			.ToArray();
		string[] expected = ExpectedGroups.Where(entry => entry.Value == group)
			.Select(entry => entry.Key)
			.Order(StringComparer.Ordinal)
			.ToArray();

		Assert.Equal(expected, patterns);
	}

	[Theory]
	[MemberData(nameof(PublishedTools.ToolNames), MemberType = typeof(PublishedTools))]
	public void WhenAnEndpointIsListed_ThenItsDescriptionIsTheToolDescription(string toolName)
	{
		Tool tool = PublishedTools.Named(toolName);
		JsonElement endpoint = PublishedTools.Endpoint(toolName);

		Assert.Equal(tool.Description, endpoint.GetProperty("description").GetString());
	}

	[Theory]
	[MemberData(nameof(PublishedTools.ToolNames), MemberType = typeof(PublishedTools))]
	public void WhenAnEndpointIsListed_ThenItsOptionsAreTheToolParameters(string toolName)
	{
		Tool tool = PublishedTools.Named(toolName);
		IReadOnlyDictionary<string, JsonElement> parameters = PublishedTools.Properties(tool);
		IReadOnlySet<string> required = PublishedTools.Required(tool);
		Dictionary<string, JsonElement> options = PublishedTools.Endpoint(toolName).GetProperty("options").EnumerateArray()
			.ToDictionary(option => option.GetProperty("name").GetString()!, StringComparer.Ordinal);

		Assert.Equal(parameters.Keys.Order(StringComparer.Ordinal), options.Keys.Order(StringComparer.Ordinal));
		Assert.Empty(PublishedTools.Endpoint(toolName).GetProperty("parameters").EnumerateArray());

		foreach ((string name, JsonElement parameter) in parameters)
		{
			JsonElement option = options[name];
			Assert.True(
				required.Contains(name) == option.GetProperty("required").GetBoolean(),
				$"{toolName}.{name}: MCP required={required.Contains(name)}, CLI required={option.GetProperty("required").GetBoolean()}.");

			// WithRoslynkTools moves a JSON Schema default into the description as a 'Default: <value>' suffix.
			(string description, string? mcpDefault) = SplitDefault(parameter.GetProperty("description").GetString()!);
			Assert.Equal(description, option.GetProperty("description").GetString());

			string? cliDefault = option.TryGetProperty("defaultValue", out JsonElement value) ? value.GetString() : null;
			Assert.True(
				string.Equals(mcpDefault, cliDefault, StringComparison.OrdinalIgnoreCase),
				$"{toolName}.{name}: MCP default '{mcpDefault}', CLI default '{cliDefault}'.");
		}
	}

	private static (string Description, string? Default) SplitDefault(string description)
	{
		Match match = DefaultSuffix().Match(description);
		return match.Success
			? (description[..match.Index], match.Groups["value"].Value.Trim('"'))
			: (description, null);
	}

	[GeneratedRegex(@"\s*Default: (?<value>[^\r\n]*)$")]
	private static partial Regex DefaultSuffix();
}
