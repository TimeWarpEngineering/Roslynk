using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Morris.Roslynk;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

/// <summary>
/// The MCP tool definitions exactly as the daemon publishes them (<c>AddRoslynk</c> +
/// <c>WithRoslynkTools</c>, the composition the McpTests pin), and the CLI's own catalog beside them.
/// </summary>
internal static class PublishedTools
{
	private static readonly Lazy<IReadOnlyList<Tool>> AllTools = new(() =>
	{
		var services = new ServiceCollection();
		services.AddRoslynk();
		services.AddMcpServer().WithRoslynkTools();
		using ServiceProvider provider = services.BuildServiceProvider();
		return provider.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool).ToList();
	});

	private static readonly Lazy<JsonDocument> CliCapabilities = new(() =>
	{
		CliResult result = CliProcess.RunAsync(["--capabilities"], CliProcess.UnusedPort()).GetAwaiter().GetResult();
		if (result.ExitCode != 0)
			throw new InvalidOperationException($"roslynk-cli --capabilities exited {result.ExitCode}: {result.StandardError}");
		return JsonDocument.Parse(result.StandardOutput);
	});

	public static IReadOnlyList<Tool> Tools => AllTools.Value;

	public static Tool Named(string name) => Tools.Single(tool => tool.Name == name);

	public static IEnumerable<object[]> ToolNames() => Tools.Select(tool => new object[] { tool.Name }).OrderBy(row => (string)row[0], StringComparer.Ordinal);

	public static JsonElement Capabilities => CliCapabilities.Value.RootElement;

	public static JsonElement Endpoint(string pattern) =>
		Capabilities.GetProperty("endpoints").EnumerateArray().Single(endpoint => endpoint.GetProperty("pattern").GetString() == pattern);

	public static IReadOnlyDictionary<string, JsonElement> Properties(Tool tool) =>
		tool.InputSchema.TryGetProperty("properties", out JsonElement properties)
			? properties.EnumerateObject().ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal)
			: new Dictionary<string, JsonElement>();

	public static IReadOnlySet<string> Required(Tool tool) =>
		tool.InputSchema.TryGetProperty("required", out JsonElement required)
			? required.EnumerateArray().Select(name => name.GetString()!).ToHashSet(StringComparer.Ordinal)
			: new HashSet<string>(StringComparer.Ordinal);
}
