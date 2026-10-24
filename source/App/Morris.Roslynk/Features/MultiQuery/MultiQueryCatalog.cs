using System.Collections.Frozen;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Operations;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Results;
using TimeWarp.Contracts;
using Morris.Roslynk.Infrastructure.Tools;
using Morris.Roslynk.Features.Symbols.GetSymbol;
using Morris.Roslynk.Features.Symbols.GetSymbolBody;
using Morris.Roslynk.Features.Symbols.GetMembers;
using Morris.Roslynk.Features.Symbols.FindDefinition;
using Morris.Roslynk.Features.Symbols.GetExpressionInfo;
using Morris.Roslynk.Features.Symbols.FindImplementations;
using Morris.Roslynk.Features.Symbols.GetTypeHierarchy;
using Morris.Roslynk.Features.Symbols.SearchSymbols;
using Morris.Roslynk.Features.References.FindReferences;
using Morris.Roslynk.Features.References.FindReads;
using Morris.Roslynk.Features.References.FindWrites;
using Morris.Roslynk.Features.Callers.GetCallers;
using Morris.Roslynk.Features.Callers.GetCallees;
using Morris.Roslynk.Features.DeadCode.FindDeadCode;
using Morris.Roslynk.Features.Conditionals.FindDeadConditionals;

namespace Morris.Roslynk.Features.MultiQuery;

/// <summary>
/// The multi_query allow-list: which tools an operation may invoke, and how. Deliberately hand-written —
/// never derived by reflecting over [McpServerToolType] — because the set is a contract (the schema's tool
/// enum advertises exactly these), not whatever happens to be published. One entry per tool; a new tool
/// joins by adding a line here plus an enum member.
/// </summary>
/// <remarks>
/// The invoke path is one shared reflective binder (see <see cref="MultiQueryCatalog.InvokeCoreAsync"/>).
/// Catalog entries point at the handler that takes the operation request. The binder supplies the pinned
/// <see cref="SolutionModel"/>, <see cref="RoslynInstance"/>, the slot's <see cref="ResponseBudget"/> and the
/// <see cref="CancellationToken"/> itself, and binds the caller's arguments onto that request's properties
/// (never <c>solutionId</c>, which the batch already carries). Omitted properties keep the request's
/// initializer, unknown keys are rejected as UnknownArgument, and a handler outcome is rendered at this
/// edge. With every user-facing property being string/string?/bool/int/int?/string[], no per-tool binding
/// code exists to drift; a string array accepts a real JSON array or a JSON-array string, and anything else
/// is a caller error. A target that has no <c>[Operation]</c> request — an internal core, or a test stub —
/// keeps the older parameter binder.
/// </remarks>
internal static class MultiQueryCatalog
{
	/// <summary>Constructs the tool from the engine's services, as the roslynk app's handlers resolve tools.</summary>
	public static object ConstructTool(IServiceProvider provider, OpEntry entry) =>
		ActivatorUtilities.CreateInstance(provider, entry.ToolType);

	internal sealed record OpEntry(
		string ToolName,
		Type ToolType,
		string CoreMethod)
	{
		public MethodInfo? ResolvedMethodField;

		public MethodInfo Resolve() => ResolvedMethodField ??= ToolType.GetMethod(CoreMethod, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"multi_query catalog: '{ToolType.Name}.{CoreMethod}' could not be resolved.");
	}

	/// <summary>
	/// The 15 multi-queryable tools, each pointing at the handler that takes the pinned
	/// <see cref="SolutionModel"/> and the operation request instead of acquiring its own model. Names must
	/// equal the <see cref="MultiQueryOp"/> members (the resolution test keeps enum == catalog == the tools'
	/// name constants in lockstep).
	/// </summary>
	public static readonly FrozenDictionary<string, OpEntry> Entries = new Dictionary<string, OpEntry>(StringComparer.Ordinal)
	{
		[GetSymbolTool.GetSymbolName] = new(GetSymbolTool.GetSymbolName, typeof(GetSymbolTool), nameof(GetSymbolTool.HandleAsync)),
		[GetSymbolBodyTool.GetSymbolBodyName] = new(GetSymbolBodyTool.GetSymbolBodyName, typeof(GetSymbolBodyTool), nameof(GetSymbolBodyTool.HandleAsync)),
		[GetMembersTool.GetMembersName] = new(GetMembersTool.GetMembersName, typeof(GetMembersTool), nameof(GetMembersTool.HandleAsync)),
		[FindDefinitionTool.FindDefinitionName] = new(FindDefinitionTool.FindDefinitionName, typeof(FindDefinitionTool), nameof(FindDefinitionTool.HandleAsync)),
		[FindImplementationsTool.FindImplementationsName] = new(FindImplementationsTool.FindImplementationsName, typeof(FindImplementationsTool), nameof(FindImplementationsTool.HandleAsync)),
		[FindReferencesTool.FindReferencesName] = new(FindReferencesTool.FindReferencesName, typeof(FindReferencesTool), nameof(FindReferencesTool.HandleAsync)),
		[GetCallersTool.GetCallersName] = new(GetCallersTool.GetCallersName, typeof(GetCallersTool), nameof(GetCallersTool.HandleAsync)),
		[GetCalleesTool.GetCalleesName] = new(GetCalleesTool.GetCalleesName, typeof(GetCalleesTool), nameof(GetCalleesTool.HandleAsync)),
		[SearchSymbolsTool.SearchSymbolsName] = new(SearchSymbolsTool.SearchSymbolsName, typeof(SearchSymbolsTool), nameof(SearchSymbolsTool.HandleAsync)),
		[GetTypeHierarchyTool.GetTypeHierarchyName] = new(GetTypeHierarchyTool.GetTypeHierarchyName, typeof(GetTypeHierarchyTool), nameof(GetTypeHierarchyTool.HandleAsync)),
		[FindDeadCodeTool.FindDeadCodeName] = new(FindDeadCodeTool.FindDeadCodeName, typeof(FindDeadCodeTool), nameof(FindDeadCodeTool.HandleAsync)),
		[FindDeadConditionalsTool.FindDeadConditionalsName] = new(FindDeadConditionalsTool.FindDeadConditionalsName, typeof(FindDeadConditionalsTool), nameof(FindDeadConditionalsTool.HandleAsync)),
		[GetExpressionInfoTool.GetExpressionInfoName] = new(GetExpressionInfoTool.GetExpressionInfoName, typeof(GetExpressionInfoTool), nameof(GetExpressionInfoTool.HandleAsync)),
		[FindReadsTool.FindReadsName] = new(FindReadsTool.FindReadsName, typeof(FindReadsTool), nameof(FindReadsTool.HandleAsync)),
		[FindWritesTool.FindWritesName] = new(FindWritesTool.FindWritesName, typeof(FindWritesTool), nameof(FindWritesTool.HandleAsync)),
	}.ToFrozenDictionary(StringComparer.Ordinal);

	/// <summary>
	/// Binds <paramref name="arguments"/> and invokes the entry on the tool instance built from
	/// <paramref name="provider"/>. The pinned model/instance, the slot's budget allowance and the batch's
	/// token are supplied here — never taken from the caller's arguments, which is the line that makes the
	/// snapshot guarantee real. <paramref name="allowance"/> is null when the handler runs stand-alone.
	/// A handler that takes an <see cref="OperationAttribute"/> request gets those arguments on the request.
	/// Anything else is bound parameter-by-parameter, which is how the internal cores and the test stubs run.
	/// </summary>
	/// <exception cref="UnknownArgumentException">An argument key is not one of the entry's bindable
	/// parameters. Mapped to slot error=UnknownArgument.</exception>
	/// <exception cref="ArgumentException">A required argument is missing, or a value cannot be coerced to
	/// the parameter's type. Mapped to slot error=Invalid.</exception>
	public static async Task<string> InvokeCoreAsync(
		IServiceProvider provider,
		OpEntry entry,
		SolutionModel model,
		RoslynInstance instance,
		ResponseBudget? allowance,
		IReadOnlyDictionary<string, JsonElement> arguments,
		CancellationToken token)
	{
		object tool = ConstructTool(provider, entry);
		MethodInfo core = entry.Resolve();
		ParameterInfo[] parameters = core.GetParameters();
		ParameterInfo? requestParameter = parameters.FirstOrDefault(parameter => IsOperationRequest(parameter.ParameterType));
		object?[] args = requestParameter is null
			? BindParameters(parameters, model, instance, allowance, arguments, token, entry.ToolName)
			: BindOperation(parameters, requestParameter, model, instance, allowance, arguments, token, entry.ToolName);

		object? result = Invoke(core, tool, args);
		if (result is Task<string> textTask)
			return await textTask.ConfigureAwait(false);

		if (result is Task task)
		{
			await task.ConfigureAwait(false);
			object? value = task.GetType().GetProperty("Result")!.GetValue(task);
			return OperationResults.RenderOutcome(value!, provider);
		}

		throw new InvalidOperationException($"multi_query catalog: '{entry.ToolType.Name}.{core.Name}' did not return a task.");
	}

	private static object?[] BindOperation(
		ParameterInfo[] parameters,
		ParameterInfo requestParameter,
		SolutionModel model,
		RoslynInstance instance,
		ResponseBudget? allowance,
		IReadOnlyDictionary<string, JsonElement> arguments,
		CancellationToken token,
		string toolName)
	{
		object request = BindRequest(requestParameter.ParameterType, arguments, toolName);
		var args = new object?[parameters.Length];
		for (int index = 0; index < parameters.Length; index++)
		{
			ParameterInfo parameter = parameters[index];
			if (parameter.ParameterType == typeof(SolutionModel))
				args[index] = model;
			else if (parameter.ParameterType == typeof(RoslynInstance))
				args[index] = instance;
			else if (parameter.ParameterType == typeof(ResponseBudget))
				args[index] = allowance;
			else if (parameter.ParameterType == typeof(CancellationToken))
				args[index] = token;
			else if (parameter == requestParameter)
				args[index] = request;
			else if (parameter.HasDefaultValue)
				args[index] = parameter.DefaultValue;
			else
				throw new ArgumentException($"Missing required parameter '{parameter.Name}' for '{toolName}'.");
		}

		return args;
	}

	/// <summary>
	/// Fills a request from <paramref name="arguments"/>. <c>solutionId</c> is not an operation argument:
	/// the batch carries it, and passing it here is the same unknown-key error as any other stranger.
	/// </summary>
	private static object BindRequest(Type requestType, IReadOnlyDictionary<string, JsonElement> arguments, string toolName)
	{
		object request = Activator.CreateInstance(requestType)
			?? throw new InvalidOperationException($"multi_query catalog: '{requestType.Name}' could not be constructed.");

		PropertyInfo[] properties = requestType
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(property => property.CanWrite && !string.Equals(property.Name, "solutionId", StringComparison.Ordinal))
			.ToArray();

		UnknownArgument.Reject(arguments.Keys, properties.Select(property => property.Name).ToList(), toolName);

		foreach (PropertyInfo property in properties)
		{
			bool required = property.GetCustomAttribute<RequiredMemberAttribute>() is not null;
			if (!arguments.TryGetValue(property.Name, out JsonElement value))
			{
				if (required)
					throw new ArgumentException($"Missing required parameter '{property.Name}' for '{toolName}'.");
				continue;
			}

			property.SetValue(request, Coerce(property.PropertyType, value, toolName, property.Name));
		}

		return request;
	}

	private static object?[] BindParameters(
		ParameterInfo[] parameters,
		SolutionModel model,
		RoslynInstance instance,
		ResponseBudget? allowance,
		IReadOnlyDictionary<string, JsonElement> arguments,
		CancellationToken token,
		string toolName)
	{
		// Unknown keys first: the caller's actual mistake is the one to name, before any missing-required
		// error can mask it. The same shared check the published-tool wrapper applies, so a misspelled
		// argument reads identically whether it arrives alone or inside a batch.
		var bindable = new List<string>();
		foreach (ParameterInfo parameter in parameters)
		{
			if (IsSupplied(parameter.ParameterType))
				continue;
			bindable.Add(parameter.Name!);
		}
		UnknownArgument.Reject(arguments.Keys, bindable, toolName);

		var args = new object?[parameters.Length];
		for (int index = 0; index < parameters.Length; index++)
		{
			ParameterInfo parameter = parameters[index];
			if (parameter.ParameterType == typeof(SolutionModel))
			{
				args[index] = model;
				continue;
			}
			if (parameter.ParameterType == typeof(RoslynInstance))
			{
				args[index] = instance;
				continue;
			}
			if (parameter.ParameterType == typeof(ResponseBudget))
			{
				args[index] = allowance;
				continue;
			}
			if (parameter.ParameterType == typeof(CancellationToken))
			{
				args[index] = token;
				continue;
			}

			if (!arguments.TryGetValue(parameter.Name!, out JsonElement value))
			{
				if (!parameter.HasDefaultValue)
					throw new ArgumentException($"Missing required parameter '{parameter.Name}' for '{toolName}'.");
				args[index] = parameter.DefaultValue;
				continue;
			}

			args[index] = Coerce(parameter.ParameterType, value, toolName, parameter.Name!);
		}

		return args;
	}

	private static bool IsOperationRequest(Type type) =>
		type.GetCustomAttribute<OperationAttribute>() is not null;

	// MethodInfo.Invoke wraps any handler exception in TargetInvocationException; unwrap it so the per-op
	// boundary maps the handler's own exception (and carries its message), not the reflection wrapper.
	private static object? Invoke(MethodInfo method, object tool, object?[] args)
	{
		try
		{
			return method.Invoke(tool, args);
		}
		catch (TargetInvocationException invocation)
			when (invocation.InnerException is not null)
		{
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(invocation.InnerException).Throw();
			throw; // unreachable
		}
	}

	/// <summary>
	/// The parameter types the binder itself supplies, never read from the caller's arguments. A core
	/// declaring a <see cref="ResponseBudget"/> receives its slot's remaining character budget (null when it
	/// runs stand-alone), so it can degrade an oversized result to a well-formed page itself.
	/// </summary>
	internal static bool IsSupplied(Type type) =>
		type == typeof(SolutionModel)
		|| type == typeof(RoslynInstance)
		|| type == typeof(ResponseBudget)
		|| type == typeof(CancellationToken);

	/// <summary>
	/// Converts a JSON argument value to the core parameter's type. MCP clients vary in how they serialize
	/// scalars, so string forms are accepted for bool/int; a string array accepts both a real JSON array and
	/// a JSON-array string. Anything the target type cannot take is a caller error
	/// (ArgumentException → slot Invalid), never a silent fallback.
	/// </summary>
	private static object? Coerce(Type type, JsonElement value, string toolName, string parameterName)
	{
		string Context() => $"Parameter '{parameterName}' of '{toolName}'";
		try
		{
			if (type == typeof(string))
				return value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText();

			if (type == typeof(bool))
			{
				if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
					return value.GetBoolean();
				if (value.ValueKind == JsonValueKind.String)
					return bool.Parse(value.GetString()!);
				throw new FormatException($"{Context()} expects true or false.");
			}

			if (type == typeof(int))
			{
				if (value.ValueKind == JsonValueKind.Number)
					return value.GetInt32();
				if (value.ValueKind == JsonValueKind.String)
					return int.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
				throw new FormatException($"{Context()} expects a number.");
			}

			if (type == typeof(int?))
			{
				if (value.ValueKind == JsonValueKind.Null)
					return null;
				return (int?)Coerce(typeof(int), value, toolName, parameterName);
			}

			if (type == typeof(string[]))
				return JsonArrayArgument.Strings(value, toolName, parameterName);

			throw new NotSupportedException(
				$"'{toolName}' exposes a parameter type ({type.Name}) the multi_query binder does not support; extend the binder.");
		}
		catch (Exception exception) when (exception is FormatException or JsonException or OverflowException)
		{
			// Re-thrown with the parameter context, since the raw message would not name the offending op.
			throw new FormatException($"{Context()} could not take {value.GetRawText()}: {exception.Message}");
		}
	}
}
