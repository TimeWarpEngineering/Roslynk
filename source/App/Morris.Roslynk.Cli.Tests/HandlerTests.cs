extern alias app;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Morris.Roslynk.Cli.Tests.Infrastructure;
using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Outlines;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using AppChangeSignatureHandler = app::Morris.Roslynk.Cli.Handlers.ChangeSignatureHandler;
using AppExitCodes = app::Morris.Roslynk.Cli.Output.ExitCodes;
using AppGetDiagnosticsHandler = app::Morris.Roslynk.Cli.Handlers.GetDiagnosticsHandler;
using AppGetSymbolHandler = app::Morris.Roslynk.Cli.Handlers.GetSymbolHandler;
using AppMultiQueryHandler = app::Morris.Roslynk.Cli.Handlers.MultiQueryHandler;
using AppOperationHandler = app::Morris.Roslynk.Cli.Handlers.OperationHandler;
using ChangeSignature = Morris.Roslynk.Features.Signatures.ChangeSignature.ChangeSignature;
using GetDiagnostics = Morris.Roslynk.Features.Diagnostics.GetDiagnostics.GetDiagnostics;
using GetSymbol = Morris.Roslynk.Features.Symbols.GetSymbol.GetSymbol;
using MultiQuery = Morris.Roslynk.Features.MultiQuery.MultiQuery;
using MultiQueryOp = Morris.Roslynk.Features.MultiQuery.MultiQueryOp;
using MultiQueryOperation = Morris.Roslynk.Features.MultiQuery.MultiQueryOperation;
using RoslynkEngine = app::Morris.Roslynk.Cli.Hosting.RoslynkEngine;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// The app's handlers as units: a request reaches the engine with only its caller paths resolved, an outline
/// maps onto the outcome with the documented exit code (an unknown error code is not a usage error), an
/// oversized result is cut to the budget, the caller's cancellation reaches the engine, and a read cancelled
/// or superseded after its snapshot was captured comes back as Stale with a snapshot header.
/// </summary>
public class HandlerTests
{
	private static readonly JsonSerializerOptions Json = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

	private static readonly Assembly App = typeof(AppOperationHandler).Assembly;

	/// <summary>A caller path relative to the caller's directory, which outside a served call is this process's.</summary>
	private static readonly string RelativeSolution = Path.Combine("Samples", "Sample.slnx");

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenARequestIsHandedToTheEngine_ThenOnlyItsCallerPathsAreResolved(string name)
	{
		OperationContract operation = Operations.Named(name);
		JsonObject sample = new();
		foreach (PropertyInfo member in operation.Members)
			sample[member.Name] = SampleOf(member);
		object request = sample.Deserialize(operation.Request, Json)!;

		MethodInfo fromCaller = typeof(AppOperationHandler).GetMethod("FromCaller", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(operation.Request);
		object handed = fromCaller.Invoke(null, [request])!;

		JsonObject expected = sample.DeepClone().AsObject();
		foreach (string member in new[] { "solutionId", "solutionPath" }.Where(expected.ContainsKey))
			expected[member] = Path.GetFullPath(RelativeSolution);
		Assert.Same(request, handed);
		Assert.Equal(expected.ToJsonString(), JsonSerializer.SerializeToNode(handed, operation.Request, Json)!.ToJsonString());
	}

	[Theory]
	[InlineData("Invalid", 2)]
	[InlineData("NotFound", 3)]
	[InlineData("Ambiguous", 4)]
	[InlineData("Indexing", 5)]
	[InlineData("Stale", 6)]
	[InlineData("Conflict", 7)]
	[InlineData("NotSupported", 8)]
	[InlineData("Faulted", 9)]
	[InlineData("Truncated", 10)]
	[InlineData("UnknownArgument", 11)]
	[InlineData("SomethingNew", 70)]
	public void WhenAnOutlineIsAProblem_ThenItsStatusIsTheDocumentedExitCode(string code, int exitCode)
	{
		var outcome = AppOperationHandler.FromText<GetSymbol.Response>($"error={code}\nerrorMessage=Something.\n", maxChars: null);

		Assert.True(outcome.IsT1);
		Assert.Equal(code, outcome.AsT1.Type);
		Assert.Equal(exitCode, outcome.AsT1.Status);
		Assert.NotEqual(AppExitCodes.Usage, AppExitCodes.For(code));
	}

	[Fact]
	public void WhenAResultExceedsTheBudget_ThenItIsCutAtALineAndMarked()
	{
		string text = string.Join('\n', Enumerable.Range(0, 2_000).Select(index => $"line {index:D6} of a long result"));

		var outcome = AppOperationHandler.FromText<GetSymbol.Response>(text, maxChars: 8_000);

		Assert.True(outcome.IsT0);
		Assert.True(outcome.AsT0.Text.Length <= 8_000);
		Assert.Contains("outputTruncated=Y", outcome.AsT0.Text);
	}

	[Fact]
	public async Task WhenTheCallerCancels_ThenTheCancellationReachesTheEngine()
	{
		using RoslynkEngine engine = new(new ConfigurationBuilder().Build());
		string solutionId = Path.Combine(Fixtures.Root, "SimpleSolution", "SimpleSolution.slnx");
		using CancellationTokenSource cancelled = new();
		await cancelled.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			new AppGetSymbolHandler(engine).Handle(new GetSymbol.Query { solutionId = solutionId, symbolName = "x" }, cancelled.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			new AppGetDiagnosticsHandler(engine).Handle(new GetDiagnostics.Query { solutionId = solutionId }, cancelled.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			new AppChangeSignatureHandler(engine).Handle(new ChangeSignature.Command { solutionId = solutionId, symbolName = "x", parameterType = "int", parameterName = "y", defaultValue = "0" }, cancelled.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			new AppMultiQueryHandler(engine).Handle(new MultiQuery.Query { solutionId = solutionId, operations = [new MultiQueryOperation(MultiQueryOp.get_symbol)] }, cancelled.Token).AsTask());
	}

	/// <summary>
	/// A read cancelled after its snapshot was captured, and a read whose solution is published again
	/// before it returns, both come back as <c>error=Stale</c> (exit 6) with a <c>snapshot=</c> header.
	/// An already-cancelled token still throws; that path is the test above.
	/// </summary>
	[Fact]
	public async Task WhenACapturedReadIsCancelledOrSuperseded_ThenTheHandlerReturnsStaleWithTheSnapshot()
	{
		string scratch = Path.Combine(Path.GetTempPath(), $"roslynk-handler-stale-{Guid.NewGuid():N}");
		Fixtures.CopyDirectory(Path.Combine(Fixtures.Root, "SimpleSolution"), scratch);
		File.Copy(Path.Combine(Fixtures.Root, "global.json"), Path.Combine(scratch, "global.json"));
		string solutionId = Path.Combine(scratch, "SimpleSolution.slnx");
		Fixtures.Restore(solutionId);

		RoslynkEngine engine = new(new ConfigurationBuilder().Build());
		try
		{
			using CancellationTokenSource cancellation = new();
			OneOf<GetSymbol.Response, SharedProblemDetails> cancelled = await ReadSymbolAsync(
				engine,
				solutionId,
				(_, _, _, token) =>
				{
					cancellation.Cancel();
					token.ThrowIfCancellationRequested();
					return Task.FromResult<OneOf<GetSymbol.Response, SharedProblemDetails>>(new GetSymbol.Response { Text = "body=never\n" });
				},
				cancellation.Token);
			AssertStaleSnapshot(cancelled);

			OneOf<GetSymbol.Response, SharedProblemDetails> superseded = await ReadSymbolAsync(
				engine,
				solutionId,
				(model, instance, _, _) =>
				{
					Assert.NotNull(model.Solution);
					instance.AdvanceTo(model.Solution
						.Projects.First()
						.AddDocument("Added.cs", "namespace Added; public class Added { }")
						.Project.Solution);
					return Task.FromResult<OneOf<GetSymbol.Response, SharedProblemDetails>>(new GetSymbol.Response { Text = "body=stale\n" });
				},
				CancellationToken.None);
			AssertStaleSnapshot(superseded);
		}
		finally
		{
			engine.Dispose();
			if (Directory.Exists(scratch))
				Directory.Delete(scratch, recursive: true);
		}
	}

	[Fact]
	public void WhenAProblemIsRendered_ThenItIsTheErrorOutline()
	{
		Type renderer = App.GetType("Morris.Roslynk.Cli.Output.ProblemRenderer")!;
		var instance = (IResponseRenderer<SharedProblemDetails>)Activator.CreateInstance(renderer)!;

		Assert.Equal("error=NotFound\nerrorMessage=Gone.\n", instance.Render(new SharedProblemDetails { Type = "NotFound", Detail = "Gone." }));
	}

	/// <summary>
	/// Calls the protected read on a real get_symbol handler. The method is constrained to the engine's
	/// internal outline response, which this assembly cannot name, so the closed method is invoked directly.
	/// </summary>
	private static async Task<OneOf<GetSymbol.Response, SharedProblemDetails>> ReadSymbolAsync(
		RoslynkEngine engine,
		string solutionId,
		Func<SolutionModel, RoslynInstance, ResponseBudget, CancellationToken, Task<OneOf<GetSymbol.Response, SharedProblemDetails>>> handle,
		CancellationToken cancellationToken)
	{
		GetSymbol.Query query = new() { solutionId = solutionId, symbolName = "x" };
		MethodInfo readAsync = typeof(AppOperationHandler).GetMethod("ReadAsync", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new Xunit.Sdk.XunitException("OperationHandler.ReadAsync was not found.");
		MethodInfo closed = readAsync.MakeGenericMethod(typeof(GetSymbol.Response));
		object invoked = closed.Invoke(new AppGetSymbolHandler(engine), [query, query.solutionId, handle, cancellationToken])
			?? throw new Xunit.Sdk.XunitException("OperationHandler.ReadAsync returned null.");
		return await (Task<OneOf<GetSymbol.Response, SharedProblemDetails>>)invoked;
	}

	/// <summary>Exit 6, type Stale, and a snapshot header that survives into the extensions and the rendered outline.</summary>
	private static void AssertStaleSnapshot(OneOf<GetSymbol.Response, SharedProblemDetails> outcome)
	{
		Assert.True(outcome.IsT1);
		SharedProblemDetails problem = outcome.AsT1;
		Assert.Equal("Stale", problem.Type);
		Assert.Equal(AppExitCodes.Stale, problem.Status);

		Assert.True(problem.Extensions.TryGetValue("headers", out object? raw));
		string? snapshot = null;
		foreach (object header in (System.Collections.IEnumerable)raw!)
		{
			if (header.GetType().GetProperty("Key")?.GetValue(header) is string key && key == "snapshot")
				snapshot = header.GetType().GetProperty("Value")?.GetValue(header) as string;
		}

		Assert.False(string.IsNullOrWhiteSpace(snapshot));
		string text = new app::Morris.Roslynk.Cli.Output.ProblemRenderer().Render(problem);
		Assert.Contains("error=Stale\n", text, StringComparison.Ordinal);
		Assert.Contains($"snapshot={snapshot}\n", text, StringComparison.Ordinal);
		Assert.DoesNotContain("body=", text, StringComparison.Ordinal);
	}

	/// <summary>A non-default JSON value for a request member, by its type.</summary>
	private static JsonNode SampleOf(PropertyInfo member)
	{
		Type type = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;
		if (member.Name is "solutionId" or "solutionPath")
			return JsonValue.Create(RelativeSolution)!;
		if (type == typeof(string))
			return JsonValue.Create($"{member.Name}-value")!;
		if (type == typeof(bool))
			return JsonValue.Create(!(bool)(member.GetValue(Activator.CreateInstance(member.DeclaringType!)) ?? false))!;
		if (type == typeof(int))
			return JsonValue.Create(7)!;
		if (type == typeof(string[]))
			return new JsonArray("CS0001", "IDE0005");
		if (type.Name.StartsWith("IReadOnlyList", StringComparison.Ordinal) && type.GetGenericArguments()[0].Name == "FileVersion")
			return new JsonArray(new JsonObject { ["path"] = "a.cs", ["version"] = "abc" });
		if (type.Name.StartsWith("IReadOnlyList", StringComparison.Ordinal) && type.GetGenericArguments()[0].Name == "MultiQueryOperation")
			return new JsonArray(new JsonObject { ["tool"] = "get_symbol", ["arguments"] = new JsonObject { ["symbolName"] = "X" } });
		throw new Xunit.Sdk.XunitException($"No sample for {member.DeclaringType}.{member.Name} ({member.PropertyType}).");
	}
}
