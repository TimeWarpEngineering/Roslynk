using System.Reflection;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;

namespace Morris.Roslynk.Infrastructure.Operations;

/// <summary>
/// Turns a handler outcome into the MCP text: a response goes through its
/// <see cref="IResponseRenderer{TResponse}"/>, a problem through <see cref="ProblemOutline"/>.
/// </summary>
internal static class OperationResults
{
	public static OneOf<TResponse, SharedProblemDetails> Split<TResponse>(string text)
		where TResponse : IOutlineResponse, new()
	{
		if (ProblemOutline.TryParse(text, out SharedProblemDetails? problem))
			return problem!;

		return new TResponse { Text = text };
	}

	public static string Render<TResponse>(OneOf<TResponse, SharedProblemDetails> outcome, IResponseRenderer<TResponse> renderer)
		where TResponse : IOutlineResponse =>
		outcome.Match(renderer.Render, ProblemOutline.Format);

	/// <summary>
	/// Renders a reflected <see cref="OneOf{T0,T1}"/> outcome. <paramref name="provider"/> may be null or
	/// may not know the renderer; the response type's nested renderer is the fallback the batch tests use.
	/// </summary>
	public static string RenderOutcome(object outcome, IServiceProvider? provider)
	{
		Type type = outcome.GetType();
		if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(OneOf<,>))
			throw new InvalidOperationException($"Expected a OneOf outcome, got '{type}'.");

		bool isResponse = (bool)type.GetProperty("IsT0")!.GetValue(outcome)!;
		if (!isResponse)
		{
			var problem = (SharedProblemDetails)type.GetProperty("AsT1")!.GetValue(outcome)!;
			return ProblemOutline.Format(problem);
		}

		object response = type.GetProperty("AsT0")!.GetValue(outcome)!;
		return RenderResponse(response, provider);
	}

	private static string RenderResponse(object response, IServiceProvider? provider)
	{
		Type responseType = response.GetType();
		Type rendererInterface = typeof(IResponseRenderer<>).MakeGenericType(responseType);
		object? renderer = provider?.GetService(rendererInterface);
		if (renderer is null)
		{
			Type? nested = responseType.DeclaringType?.GetNestedType("Renderer", BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo? instance = nested?.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
			renderer = instance?.GetValue(null);
			if (renderer is null && nested is not null)
				renderer = Activator.CreateInstance(nested);
		}

		if (renderer is null)
			throw new InvalidOperationException($"No IResponseRenderer<{responseType.Name}> is registered.");

		MethodInfo render = rendererInterface.GetMethod("Render")
			?? throw new InvalidOperationException($"IResponseRenderer<{responseType.Name}>.Render is missing.");
		return (string)render.Invoke(renderer, [response])!;
	}
}
