using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TimeWarp.Contracts;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

/// <summary>One operation as the engine declares it: the source of truth the catalog and the MCP tools are checked against.</summary>
public sealed record OperationContract(string Name, string Description, string Group, string? Title, Type Request)
{
	public bool IsCommand => Request.Name == "Command";

	public IReadOnlyList<PropertyInfo> Members => Request.GetProperties(BindingFlags.Public | BindingFlags.Instance);

	public IReadOnlyList<string> RequiredMembers =>
		Members.Where(member => member.GetCustomAttribute<RequiredMemberAttribute>() is not null).Select(member => member.Name).ToArray();
}

/// <summary>The 29 operations, and the catalog <c>roslynk --capabilities</c> prints for them.</summary>
public static class Operations
{
	public static IReadOnlyList<OperationContract> All { get; } = typeof(ServicesRegistration).Assembly.GetTypes()
		.Select(type => (Type: type, Operation: type.GetCustomAttribute<OperationAttribute>()))
		.Where(entry => entry.Operation is not null)
		.Select(entry => new OperationContract(
			entry.Operation!.Name,
			entry.Operation.Description ?? "",
			entry.Operation.Group ?? "",
			entry.Type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName,
			entry.Type))
		.OrderBy(operation => operation.Name, StringComparer.Ordinal)
		.ToArray();

	public static TheoryData<string> Names() => [.. All.Select(operation => operation.Name)];

	public static OperationContract Named(string name) => All.Single(operation => operation.Name == name);

	private static readonly Lazy<JsonElement> CatalogValue = new(LoadCatalog);

	/// <summary>The <c>--capabilities</c> document of the built app.</summary>
	public static JsonElement Catalog => CatalogValue.Value;

	public static JsonElement Endpoint(string pattern) =>
		Catalog.GetProperty("endpoints").EnumerateArray().Single(endpoint => endpoint.GetProperty("pattern").GetString() == pattern);

	private static JsonElement LoadCatalog()
	{
		ServerSocket socket = new();
		try
		{
			CliResult result = CliProcess.RunAsync(["--capabilities"], socket).GetAwaiter().GetResult();
			if (result.ExitCode != 0)
				throw new InvalidOperationException($"--capabilities failed: {result.StandardError}");
			using JsonDocument document = JsonDocument.Parse(result.StandardOutput);
			return document.RootElement.Clone();
		}
		finally
		{
			Directory.Delete(socket.Directory, recursive: true);
		}
	}
}
