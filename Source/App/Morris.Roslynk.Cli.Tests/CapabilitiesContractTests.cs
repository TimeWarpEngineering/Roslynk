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
