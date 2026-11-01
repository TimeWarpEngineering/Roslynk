using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Morris.Roslynk.Cli.Hosting;
using Morris.Roslynk.Cli.Output;
using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Observability;
using Morris.Roslynk.Infrastructure.Operations;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Tools;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Nuru;

namespace Morris.Roslynk.Cli.Handlers;

/// <summary>
/// The shared part of the 29 operation handlers. Each handler hands its request (the engine's own contract
/// type, which Nuru bound) to the engine's handler for that operation and turns the engine's outcome into the
/// app's: the outcome travels as the outline text the engine renders, and an <c>error=</c> outline becomes a
/// <see cref="SharedProblemDetails"/> whose <see cref="SharedProblemDetails.Status"/> is the CLI exit code. Handlers run where the call runs: in the served daemon, or in this process with
/// <c>--no-daemon</c>.
/// </summary>
internal abstract class OperationHandler
{
	/// <summary>Request members that name a file on the caller's side; a relative one resolves against the caller's directory.</summary>
	private static readonly string[] CallerPathMembers = ["solutionId", "solutionPath"];

	private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CallerPaths = new();

	private static readonly ConcurrentDictionary<Type, string> OperationNames = new();

	protected OperationHandler(RoslynkEngine engine)
	{
		Engine = engine;
	}

	protected RoslynkEngine Engine { get; }

	/// <summary>
	/// <paramref name="request"/> with a relative <c>solutionId</c> or <c>solutionPath</c> resolved against the
	/// caller's directory (<see cref="NuruCallContext.GetFullPath"/>), never the server's. The request is the
	/// one Nuru bound for this call, so it is updated in place.
	/// </summary>
	internal static TRequest FromCaller<TRequest>(TRequest request) where TRequest : class
	{
		PropertyInfo[] members = CallerPaths.GetOrAdd(request.GetType(), type => type
			.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(member => CallerPathMembers.Contains(member.Name, StringComparer.Ordinal) && member.PropertyType == typeof(string) && member.CanWrite)
			.ToArray());
		foreach (PropertyInfo member in members)
		{
			if (member.GetValue(request) is string path && !string.IsNullOrWhiteSpace(path))
				member.SetValue(request, NuruCallContext.GetFullPath(path));
		}

		return request;
	}

	/// <summary>
	/// Runs a read on one pinned snapshot: the instance for <paramref name="solutionId"/> (waiting, up to the
	/// configured load timeout, for an initial load in flight), its current model captured once, and the
	/// server-wide response budget, as the engine's own entry points do.
	/// </summary>
	protected Task<OneOf<TResponse, SharedProblemDetails>> ReadAsync<TResponse>(
		object request,
		string solutionId,
		Func<SolutionModel, RoslynInstance, ResponseBudget, CancellationToken, Task<OneOf<TResponse, SharedProblemDetails>>> handle,
		CancellationToken cancellationToken)
		where TResponse : class, IOutlineResponse, new() =>
		RunAsync<TResponse>(request, async () =>
		{
			RoslynInstance instance = await Engine.Registry.GetOrBeginAsync(NuruCallContext.GetFullPath(solutionId), cancellationToken);
			SolutionModel model = await instance.ReadModelAsync(cancellationToken);
			return await handle(model, instance, Engine.Budget, cancellationToken);
		});

	/// <summary>
	/// Runs an engine handler for <paramref name="request"/> in a span named after its operation, and maps the
	/// outcome. An exception that escapes it is Invalid or Faulted, by the rule multi_query applies per
	/// operation; cancellation is rethrown. A result over the response budget is cut after its last whole line
	/// (the backstop for the tools that cannot page), except where <paramref name="fitToBudget"/> is false:
	/// multi_query enforces the budget itself, whole or absent.
	/// </summary>
	protected async Task<OneOf<TResponse, SharedProblemDetails>> RunAsync<TResponse>(
		object request,
		Func<Task<OneOf<TResponse, SharedProblemDetails>>> handle,
		bool fitToBudget = true)
		where TResponse : class, IOutlineResponse, new()
	{
		using Activity? activity = RoslynkActivitySource.Instance.StartActivity(OperationName(request));
		string text;
		try
		{
			OneOf<TResponse, SharedProblemDetails> outcome = await handle();
			text = outcome.Match(response => response.Text, ProblemOutline.Format);
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception exception)
		{
			text = OutlineError.Format(ToolErrors.ToError(exception), SolutionStatus.Ready);
		}

		return FromText<TResponse>(text, fitToBudget ? Engine.Budget.MaxChars : null);
	}

	/// <summary>The <c>[Operation]</c> name of a request type, such as <c>get_symbol</c>.</summary>
	internal static string OperationName(object request) =>
		OperationNames.GetOrAdd(request.GetType(), type => type.GetCustomAttribute<OperationAttribute>()?.Name ?? type.Name);

	/// <summary>The app outcome for an outline: a problem with its exit code, or the response.</summary>
	internal static OneOf<TResponse, SharedProblemDetails> FromText<TResponse>(string text, int? maxChars)
		where TResponse : class, IOutlineResponse, new()
	{
		if (ProblemOutline.TryParse(text, out SharedProblemDetails? problem) && problem is not null)
		{
			problem.Status = ExitCodes.For(problem.Type);
			return problem;
		}

		if (maxChars is int max && text.Length > max)
			text = ResponseBudget.Fit(text, max);
		return new TResponse { Text = text };
	}
}
