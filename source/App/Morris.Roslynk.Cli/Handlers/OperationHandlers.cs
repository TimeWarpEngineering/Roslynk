using Morris.Roslynk.Cli.Hosting;
using Morris.Roslynk.Features.Callers.GetCallees;
using Morris.Roslynk.Features.Callers.GetCallers;
using Morris.Roslynk.Features.CodeActions.ApplyCodeAction;
using Morris.Roslynk.Features.CodeActions.ApplyCodeFix;
using Morris.Roslynk.Features.CodeActions.GetCodeActions;
using Morris.Roslynk.Features.Conditionals.FindDeadConditionals;
using Morris.Roslynk.Features.DeadCode.FindDeadCode;
using Morris.Roslynk.Features.Diagnostics.GetDiagnostics;
using Morris.Roslynk.Features.MultiQuery;
using Morris.Roslynk.Features.Patching.ApplyPatch;
using Morris.Roslynk.Features.Refactorings.ExtractMethod;
using Morris.Roslynk.Features.References.FindReads;
using Morris.Roslynk.Features.References.FindReferences;
using Morris.Roslynk.Features.References.FindWrites;
using Morris.Roslynk.Features.References.RenameSymbol;
using Morris.Roslynk.Features.Signatures.ChangeSignature;
using Morris.Roslynk.Features.Signatures.RenameParameter;
using Morris.Roslynk.Features.Solutions.GetSolutionStatus;
using Morris.Roslynk.Features.Solutions.OpenSolution;
using Morris.Roslynk.Features.Solutions.ReloadSolution;
using Morris.Roslynk.Features.Symbols.FindDefinition;
using Morris.Roslynk.Features.Symbols.FindImplementations;
using Morris.Roslynk.Features.Symbols.GetExpressionInfo;
using Morris.Roslynk.Features.Symbols.GetMembers;
using Morris.Roslynk.Features.Symbols.GetSymbol;
using Morris.Roslynk.Features.Symbols.GetSymbolBody;
using Morris.Roslynk.Features.Symbols.GetTypeHierarchy;
using Morris.Roslynk.Features.Symbols.SearchSymbols;
using Morris.Roslynk.Features.Usings.RemoveUnusedUsings;
using OneOf;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;

namespace Morris.Roslynk.Cli.Handlers;

// One handler per [Operation]: it hands the request to the engine's handler for that operation (see
// OperationHandler). Reads take the pinned snapshot and the response budget; writes, code actions, diagnostics
// and multi_query take the request and pin or queue inside the engine. A new operation needs its line here
// (see AGENTS.md).

internal sealed class ApplyCodeActionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<ApplyCodeAction.Command, OneOf<ApplyCodeAction.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<ApplyCodeAction.Response, SharedProblemDetails>> Handle(ApplyCodeAction.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<ApplyCodeAction.Response>(command, () =>
			Engine.Tool<ApplyCodeActionTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class ApplyCodeFixHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<ApplyCodeFix.Command, OneOf<ApplyCodeFix.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<ApplyCodeFix.Response, SharedProblemDetails>> Handle(ApplyCodeFix.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<ApplyCodeFix.Response>(command, () =>
			Engine.Tool<ApplyCodeFixTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class ApplyPatchHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<ApplyPatch.Command, OneOf<ApplyPatch.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<ApplyPatch.Response, SharedProblemDetails>> Handle(ApplyPatch.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<ApplyPatch.Response>(command, () =>
			Engine.Tool<ApplyPatchTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class ChangeSignatureHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<ChangeSignature.Command, OneOf<ChangeSignature.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<ChangeSignature.Response, SharedProblemDetails>> Handle(ChangeSignature.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<ChangeSignature.Response>(command, () =>
			Engine.Tool<ChangeSignatureTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class ExtractMethodHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<ExtractMethod.Command, OneOf<ExtractMethod.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<ExtractMethod.Response, SharedProblemDetails>> Handle(ExtractMethod.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<ExtractMethod.Response>(command, () =>
			Engine.Tool<ExtractMethodTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class FindDeadCodeHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindDeadCode.Query, OneOf<FindDeadCode.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindDeadCode.Response, SharedProblemDetails>> Handle(FindDeadCode.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindDeadCode.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindDeadCodeTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class FindDeadConditionalsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindDeadConditionals.Query, OneOf<FindDeadConditionals.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindDeadConditionals.Response, SharedProblemDetails>> Handle(FindDeadConditionals.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindDeadConditionals.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindDeadConditionalsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class FindDefinitionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindDefinition.Query, OneOf<FindDefinition.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindDefinition.Response, SharedProblemDetails>> Handle(FindDefinition.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindDefinition.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindDefinitionTool>().HandleAsync(model, instance, FromCaller(query), token), cancellationToken));
}

internal sealed class FindImplementationsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindImplementations.Query, OneOf<FindImplementations.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindImplementations.Response, SharedProblemDetails>> Handle(FindImplementations.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindImplementations.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindImplementationsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class FindReadsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindReads.Query, OneOf<FindReads.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindReads.Response, SharedProblemDetails>> Handle(FindReads.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindReads.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindReadsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class FindReferencesHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindReferences.Query, OneOf<FindReferences.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindReferences.Response, SharedProblemDetails>> Handle(FindReferences.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindReferences.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindReferencesTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class FindWritesHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<FindWrites.Query, OneOf<FindWrites.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<FindWrites.Response, SharedProblemDetails>> Handle(FindWrites.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<FindWrites.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindWritesTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class GetCalleesHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetCallees.Query, OneOf<GetCallees.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetCallees.Response, SharedProblemDetails>> Handle(GetCallees.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetCallees.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetCalleesTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class GetCallersHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetCallers.Query, OneOf<GetCallers.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetCallers.Response, SharedProblemDetails>> Handle(GetCallers.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetCallers.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetCallersTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class GetCodeActionsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetCodeActions.Query, OneOf<GetCodeActions.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetCodeActions.Response, SharedProblemDetails>> Handle(GetCodeActions.Query query, CancellationToken cancellationToken) =>
		new(RunAsync<GetCodeActions.Response>(query, () =>
			Engine.Tool<GetCodeActionsTool>().HandleAsync(FromCaller(query), cancellationToken)));
}

internal sealed class GetDiagnosticsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetDiagnostics.Query, OneOf<GetDiagnostics.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetDiagnostics.Response, SharedProblemDetails>> Handle(GetDiagnostics.Query query, CancellationToken cancellationToken) =>
		new(RunAsync<GetDiagnostics.Response>(query, () =>
			Engine.Tool<GetDiagnosticsTool>().HandleAsync(FromCaller(query), cancellationToken)));
}

internal sealed class GetExpressionInfoHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetExpressionInfo.Query, OneOf<GetExpressionInfo.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetExpressionInfo.Response, SharedProblemDetails>> Handle(GetExpressionInfo.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetExpressionInfo.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetExpressionInfoTool>().HandleAsync(model, instance, FromCaller(query), token), cancellationToken));
}

internal sealed class GetMembersHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetMembers.Query, OneOf<GetMembers.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetMembers.Response, SharedProblemDetails>> Handle(GetMembers.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetMembers.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetMembersTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class GetSolutionStatusHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetSolutionStatus.Query, OneOf<GetSolutionStatus.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetSolutionStatus.Response, SharedProblemDetails>> Handle(GetSolutionStatus.Query query, CancellationToken cancellationToken) =>
		new(RunAsync<GetSolutionStatus.Response>(query, () =>
			Task.FromResult(Engine.Tool<GetSolutionStatusTool>().Handle(FromCaller(query)))));
}

internal sealed class GetSymbolHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetSymbol.Query, OneOf<GetSymbol.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetSymbol.Response, SharedProblemDetails>> Handle(GetSymbol.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetSymbol.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetSymbolTool>().HandleAsync(model, instance, FromCaller(query), token), cancellationToken));
}

internal sealed class GetSymbolBodyHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetSymbolBody.Query, OneOf<GetSymbolBody.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetSymbolBody.Response, SharedProblemDetails>> Handle(GetSymbolBody.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetSymbolBody.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetSymbolBodyTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class GetTypeHierarchyHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<GetTypeHierarchy.Query, OneOf<GetTypeHierarchy.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<GetTypeHierarchy.Response, SharedProblemDetails>> Handle(GetTypeHierarchy.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<GetTypeHierarchy.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetTypeHierarchyTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}

internal sealed class MultiQueryHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<MultiQuery.Query, OneOf<MultiQuery.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<MultiQuery.Response, SharedProblemDetails>> Handle(MultiQuery.Query query, CancellationToken cancellationToken) =>
		new(RunAsync<MultiQuery.Response>(query, () =>
			Engine.Tool<MultiQueryTool>().HandleAsync(FromCaller(query), cancellationToken), fitToBudget: false));
}

internal sealed class OpenSolutionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<OpenSolution.Query, OneOf<OpenSolution.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<OpenSolution.Response, SharedProblemDetails>> Handle(OpenSolution.Query query, CancellationToken cancellationToken) =>
		new(RunAsync<OpenSolution.Response>(query, () =>
			Engine.Tool<OpenSolutionTool>().HandleAsync(FromCaller(query), cancellationToken)));
}

internal sealed class ReloadSolutionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<ReloadSolution.Query, OneOf<ReloadSolution.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<ReloadSolution.Response, SharedProblemDetails>> Handle(ReloadSolution.Query query, CancellationToken cancellationToken) =>
		new(RunAsync<ReloadSolution.Response>(query, () =>
			Task.FromResult(Engine.Tool<ReloadSolutionTool>().Handle(FromCaller(query)))));
}

internal sealed class RemoveUnusedUsingsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<RemoveUnusedUsings.Command, OneOf<RemoveUnusedUsings.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<RemoveUnusedUsings.Response, SharedProblemDetails>> Handle(RemoveUnusedUsings.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<RemoveUnusedUsings.Response>(command, () =>
			Engine.Tool<RemoveUnusedUsingsTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class RenameParameterHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<RenameParameter.Command, OneOf<RenameParameter.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<RenameParameter.Response, SharedProblemDetails>> Handle(RenameParameter.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<RenameParameter.Response>(command, () =>
			Engine.Tool<RenameParameterTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class RenameSymbolHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<RenameSymbol.Command, OneOf<RenameSymbol.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<RenameSymbol.Response, SharedProblemDetails>> Handle(RenameSymbol.Command command, CancellationToken cancellationToken) =>
		new(RunAsync<RenameSymbol.Response>(command, () =>
			Engine.Tool<RenameSymbolTool>().HandleAsync(FromCaller(command), cancellationToken)));
}

internal sealed class SearchSymbolsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IRequestHandler<SearchSymbols.Query, OneOf<SearchSymbols.Response, SharedProblemDetails>>
{
	public ValueTask<OneOf<SearchSymbols.Response, SharedProblemDetails>> Handle(SearchSymbols.Query query, CancellationToken cancellationToken) =>
		new(ReadAsync<SearchSymbols.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<SearchSymbolsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken));
}
