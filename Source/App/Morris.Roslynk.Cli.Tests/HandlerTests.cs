extern alias app;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Morris.Roslynk.Cli.Tests.Infrastructure;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using AppExitCodes = app::Morris.Roslynk.Cli.Output.ExitCodes;
using AppGetDiagnostics = app::Morris.Roslynk.Features.Diagnostics.GetDiagnostics.GetDiagnostics;
using AppGetDiagnosticsHandler = app::Morris.Roslynk.Cli.Handlers.GetDiagnosticsHandler;
using AppGetSymbol = app::Morris.Roslynk.Features.Symbols.GetSymbol.GetSymbol;
using AppGetSymbolHandler = app::Morris.Roslynk.Cli.Handlers.GetSymbolHandler;
using AppMultiQuery = app::Morris.Roslynk.Features.MultiQuery.MultiQuery;
using AppMultiQueryHandler = app::Morris.Roslynk.Cli.Handlers.MultiQueryHandler;
using AppMultiQueryOp = app::Morris.Roslynk.Features.MultiQuery.MultiQueryOp;
using AppMultiQueryOperation = app::Morris.Roslynk.Features.MultiQuery.MultiQueryOperation;
using AppOperationHandler = app::Morris.Roslynk.Cli.Handlers.OperationHandler;
using RoslynkEngine = app::Morris.Roslynk.Cli.Hosting.RoslynkEngine;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// The app's handlers as units: every request copies onto the engine's contract type member for member, an
/// outline maps onto the outcome with the documented exit code (an unknown error code is not a usage error),
/// an oversized result is cut to the budget, and the caller's cancellation reaches the engine.
/// </summary>
public class HandlerTests
{
	private static readonly JsonSerializerOptions Json = new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

	private static readonly Assembly App = typeof(AppOperationHandler).Assembly;

	[Theory]
	[MemberData(nameof(Operations.Names), MemberType = typeof(Operations))]
	public void WhenARequestIsHandedToTheEngine_ThenEveryMemberIsCopied(string name)
	{
		OperationContract operation = Operations.Named(name);
		Type appRequest = App.GetType(operation.Request.FullName!) ?? throw new Xunit.Sdk.XunitException($"The app has no '{operation.Request.FullName}'.");
		JsonObject sample = new();
		foreach (PropertyInfo member in operation.Members)
			sample[member.Name] = SampleOf(member);
		object request = sample.Deserialize(appRequest, Json)!;

		MethodInfo toEngine = typeof(AppOperationHandler).GetMethod("ToEngine", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(operation.Request);
		object copied = toEngine.Invoke(null, [request])!;

		Assert.Equal(sample.ToJsonString(), JsonSerializer.SerializeToNode(copied, operation.Request, Json)!.ToJsonString());
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
	[InlineData("SomethingNew", 70)]
	public void WhenAnOutlineIsAProblem_ThenItsStatusIsTheDocumentedExitCode(string code, int exitCode)
	{
		var outcome = AppOperationHandler.FromText<AppGetSymbol.Response>($"error={code}\nerrorMessage=Something.\n", maxChars: null);

		Assert.True(outcome.IsT1);
		Assert.Equal(code, outcome.AsT1.Type);
		Assert.Equal(exitCode, outcome.AsT1.Status);
		Assert.NotEqual(AppExitCodes.Usage, AppExitCodes.For(code));
	}

	[Fact]
	public void WhenAResultExceedsTheBudget_ThenItIsCutAtALineAndMarked()
	{
		string text = string.Join('\n', Enumerable.Range(0, 2_000).Select(index => $"line {index:D6} of a long result"));

		var outcome = AppOperationHandler.FromText<AppGetSymbol.Response>(text, maxChars: 8_000);

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
			new AppGetSymbolHandler(engine).Handle(new AppGetSymbol.Query { solutionId = solutionId, symbolName = "x" }, cancelled.Token));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			new AppGetDiagnosticsHandler(engine).Handle(new AppGetDiagnostics.Query { solutionId = solutionId }, cancelled.Token));
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			new AppMultiQueryHandler(engine).Handle(new AppMultiQuery.Query { solutionId = solutionId, operations = [new AppMultiQueryOperation(AppMultiQueryOp.get_symbol)] }, cancelled.Token));
	}

	[Fact]
	public void WhenAProblemIsRendered_ThenItIsTheErrorOutline()
	{
		Type renderer = App.GetType("Morris.Roslynk.Cli.Output.ProblemRenderer")!;
		var instance = (IResponseRenderer<SharedProblemDetails>)Activator.CreateInstance(renderer)!;

		Assert.Equal("error=NotFound\nerrorMessage=Gone.\n", instance.Render(new SharedProblemDetails { Type = "NotFound", Detail = "Gone." }));
	}

	/// <summary>A non-default JSON value for a request member, by its type.</summary>
	private static JsonNode SampleOf(PropertyInfo member)
	{
		Type type = Nullable.GetUnderlyingType(member.PropertyType) ?? member.PropertyType;
		if (member.Name is "solutionId" or "solutionPath")
			return JsonValue.Create(Path.Combine(Path.GetTempPath(), "Sample.slnx"))!;
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
