using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Morris.Roslynk.Cli.Tests.Infrastructure;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// The CLI catalog is the engine's operations: the same 29 names, groups, descriptions and titles, one option
/// per request member with its required flag, plus the local-only <c>stdio</c> route. McpHost derives the MCP
/// tools from this same catalog (see <see cref="McpSurfaceTests"/>).
/// </summary>
public class CapabilitiesContractTests
{
	/// <summary>The group each operation declares on its <c>[Operation]</c>, which Nuru reports as the route's <c>groupPath</c>.</summary>
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
	public void WhenCapabilitiesAreListed_ThenThereIsOneEndpointPerOperationBesideStdio()
	{
		string[] patterns = Operations.Catalog.GetProperty("endpoints").EnumerateArray()
			.Select(endpoint => endpoint.GetProperty("pattern").GetString()!)
			.Order(StringComparer.Ordinal)
			.ToArray();

		Assert.Equal(29, Operations.All.Count);
		Assert.Equal(ExpectedGroups.Keys.Order(StringComparer.Ordinal), Operations.All.Select(operation => operation.Name));
		Assert.Equal(Operations.All.Select(operation => operation.Name).Append("stdio").Order(StringComparer.Ordinal), patterns);
	}

	[Fact]
	public void WhenCapabilitiesAreListed_ThenStdioIsLocalOnlyAndUngrouped()
	{
		JsonElement stdio = Operations.Endpoint("stdio");

		Assert.True(stdio.GetProperty("localOnly").GetBoolean());
		Assert.Empty(stdio.GetProperty("groupPath").EnumerateArray());
	}

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAnEndpointIsListed_ThenItsGroupPathIsItsOneGroup(string name)
	{
		string[] groupPath = Operations.Endpoint(name).GetProperty("groupPath").EnumerateArray()
			.Select(segment => segment.GetString()!)
			.ToArray();

		Assert.Equal([ExpectedGroups[name]], groupPath);
		Assert.Equal(ExpectedGroups[name], Operations.Named(name).Group);
	}

	[Theory]
	[MemberData(nameof(Groups))]
	public async Task WhenCapabilitiesAreFilteredByGroup_ThenOnlyThatGroupsRoutesAreListed(string group)
	{
		await using ServerSocket socket = new();
		CliResult result = await CliProcess.RunAsync(["--capabilities", "--group-filter", group], socket);

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
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAnEndpointIsListed_ThenItCarriesTheOperationsDescriptionTitleAndKind(string name)
	{
		OperationContract operation = Operations.Named(name);
		JsonElement endpoint = Operations.Endpoint(name);

		Assert.Equal(operation.Description, endpoint.GetProperty("description").GetString());
		Assert.False(string.IsNullOrWhiteSpace(operation.Title), $"{name} has no [Operation(Title)].");
		Assert.Equal(operation.Title, endpoint.GetProperty("title").GetString());
		Assert.Equal(operation.IsCommand ? "command" : "query", endpoint.GetProperty("kind").GetString());
	}

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenAnEndpointIsListed_ThenItsOptionsAreTheRequestMembers(string name)
	{
		OperationContract operation = Operations.Named(name);
		JsonElement endpoint = Operations.Endpoint(name);
		Dictionary<string, JsonElement> options = endpoint.GetProperty("options").EnumerateArray()
			.ToDictionary(option => option.GetProperty("name").GetString()!, StringComparer.Ordinal);

		Assert.Empty(endpoint.GetProperty("parameters").EnumerateArray());
		Assert.Equal(operation.Members.Select(member => member.Name).Order(StringComparer.Ordinal), options.Keys.Order(StringComparer.Ordinal));
		foreach (PropertyInfo member in operation.Members)
		{
			JsonElement option = options[member.Name];
			Assert.Equal(operation.RequiredMembers.Contains(member.Name), option.GetProperty("required").GetBoolean());
			Assert.Equal(member.GetCustomAttribute<DescriptionAttribute>()?.Description, option.GetProperty("description").GetString());
		}
	}
}
