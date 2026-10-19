using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using TimeWarp.Contracts;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// The MCP <c>tools/call</c> a bound request stands for: the <see cref="OperationAttribute"/> name and one
/// argument per public settable property, keyed by the property identifier as declared (the MCP parameter
/// name). A required member is always sent. An optional member still at its initializer is omitted, so the
/// daemon applies its own default exactly as it does for an MCP caller that leaves the parameter out.
/// </summary>
internal sealed class ForwardedCall
{
	/// <summary>
	/// Web defaults match the MCP SDK's own options, so a nested value (<c>multi_query.operations</c>,
	/// <c>apply_patch.baseVersions</c>) is written with the camelCase names the published schema declares.
	/// </summary>
	private static readonly JsonSerializerOptions ArgumentOptions = new(JsonSerializerDefaults.Web)
	{
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	private static readonly ConcurrentDictionary<Type, RequestShape> Shapes = new();

	private ForwardedCall(string toolName, Dictionary<string, JsonElement> arguments)
	{
		ToolName = toolName;
		Arguments = arguments;
	}

	public string ToolName { get; }

	public Dictionary<string, JsonElement> Arguments { get; }

	public static ForwardedCall Of(object request)
	{
		ArgumentNullException.ThrowIfNull(request);
		RequestShape shape = Shapes.GetOrAdd(request.GetType(), RequestShape.For);

		var arguments = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
		foreach (RequestMember member in shape.Members)
		{
			JsonElement value = JsonSerializer.SerializeToElement(member.Property.GetValue(request), member.Property.PropertyType, ArgumentOptions);
			if (!member.Required && member.Default is string initial && IsInitial(value, initial))
				continue;

			arguments[member.Property.Name] = value;
		}

		return new ForwardedCall(shape.ToolName, arguments);
	}

	/// <summary>
	/// Still at the initializer. Nuru binds an omitted repeated option as an empty array, so an empty array
	/// over a null initializer counts as omitted too (the daemon treats null and empty alike).
	/// </summary>
	private static bool IsInitial(JsonElement value, string initial) =>
		value.GetRawText() == initial
		|| (initial == "null" && value.ValueKind == JsonValueKind.Array && value.GetArrayLength() == 0);

	private sealed record RequestMember(PropertyInfo Property, bool Required, string? Default);

	private sealed record RequestShape(string ToolName, IReadOnlyList<RequestMember> Members)
	{
		public static RequestShape For(Type requestType)
		{
			OperationAttribute operation = requestType.GetCustomAttribute<OperationAttribute>()
				?? throw new InvalidOperationException($"{requestType.FullName} carries no [Operation].");

			// A fresh instance holds every initializer. 'required' is a compile-time rule only, so the
			// reflection constructor leaves those members unset, which is fine: they are always sent.
			object initial = Activator.CreateInstance(requestType)
				?? throw new InvalidOperationException($"{requestType.FullName} has no parameterless constructor.");

			List<RequestMember> members = requestType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(property => property.SetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
				.Select(property =>
				{
					bool required = property.IsDefined(typeof(RequiredMemberAttribute), inherit: false);
					string? initialValue = required
						? null
						: JsonSerializer.SerializeToElement(property.GetValue(initial), property.PropertyType, ArgumentOptions).GetRawText();
					return new RequestMember(property, required, initialValue);
				})
				.ToList();

			return new RequestShape(operation.Name, members);
		}
	}
}
