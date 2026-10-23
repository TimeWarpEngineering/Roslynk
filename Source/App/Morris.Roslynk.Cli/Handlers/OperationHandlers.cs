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
	: OperationHandler(engine), ICommandHandler<ApplyCodeAction.Command, OneOf<ApplyCodeAction.Response, SharedProblemDetails>>
{
	public Task<OneOf<ApplyCodeAction.Response, SharedProblemDetails>> Handle(ApplyCodeAction.Command command, CancellationToken cancellationToken) =>
		RunAsync<ApplyCodeAction.Response>(command, () =>
			Engine.Tool<ApplyCodeActionTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class ApplyCodeFixHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<ApplyCodeFix.Command, OneOf<ApplyCodeFix.Response, SharedProblemDetails>>
{
	public Task<OneOf<ApplyCodeFix.Response, SharedProblemDetails>> Handle(ApplyCodeFix.Command command, CancellationToken cancellationToken) =>
		RunAsync<ApplyCodeFix.Response>(command, () =>
			Engine.Tool<ApplyCodeFixTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class ApplyPatchHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<ApplyPatch.Command, OneOf<ApplyPatch.Response, SharedProblemDetails>>
{
	public Task<OneOf<ApplyPatch.Response, SharedProblemDetails>> Handle(ApplyPatch.Command command, CancellationToken cancellationToken) =>
		RunAsync<ApplyPatch.Response>(command, () =>
			Engine.Tool<ApplyPatchTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class ChangeSignatureHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<ChangeSignature.Command, OneOf<ChangeSignature.Response, SharedProblemDetails>>
{
	public Task<OneOf<ChangeSignature.Response, SharedProblemDetails>> Handle(ChangeSignature.Command command, CancellationToken cancellationToken) =>
		RunAsync<ChangeSignature.Response>(command, () =>
			Engine.Tool<ChangeSignatureTool>().HandleAsync(FromCaller(command)));
}

internal sealed class ExtractMethodHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<ExtractMethod.Command, OneOf<ExtractMethod.Response, SharedProblemDetails>>
{
	public Task<OneOf<ExtractMethod.Response, SharedProblemDetails>> Handle(ExtractMethod.Command command, CancellationToken cancellationToken) =>
		RunAsync<ExtractMethod.Response>(command, () =>
			Engine.Tool<ExtractMethodTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class FindDeadCodeHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindDeadCode.Query, OneOf<FindDeadCode.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindDeadCode.Response, SharedProblemDetails>> Handle(FindDeadCode.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindDeadCode.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindDeadCodeTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class FindDeadConditionalsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindDeadConditionals.Query, OneOf<FindDeadConditionals.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindDeadConditionals.Response, SharedProblemDetails>> Handle(FindDeadConditionals.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindDeadConditionals.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindDeadConditionalsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class FindDefinitionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindDefinition.Query, OneOf<FindDefinition.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindDefinition.Response, SharedProblemDetails>> Handle(FindDefinition.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindDefinition.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindDefinitionTool>().HandleAsync(model, instance, FromCaller(query), token), cancellationToken);
}

internal sealed class FindImplementationsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindImplementations.Query, OneOf<FindImplementations.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindImplementations.Response, SharedProblemDetails>> Handle(FindImplementations.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindImplementations.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindImplementationsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class FindReadsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindReads.Query, OneOf<FindReads.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindReads.Response, SharedProblemDetails>> Handle(FindReads.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindReads.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindReadsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class FindReferencesHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindReferences.Query, OneOf<FindReferences.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindReferences.Response, SharedProblemDetails>> Handle(FindReferences.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindReferences.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindReferencesTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class FindWritesHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<FindWrites.Query, OneOf<FindWrites.Response, SharedProblemDetails>>
{
	public Task<OneOf<FindWrites.Response, SharedProblemDetails>> Handle(FindWrites.Query query, CancellationToken cancellationToken) =>
		ReadAsync<FindWrites.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<FindWritesTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class GetCalleesHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetCallees.Query, OneOf<GetCallees.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetCallees.Response, SharedProblemDetails>> Handle(GetCallees.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetCallees.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetCalleesTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class GetCallersHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetCallers.Query, OneOf<GetCallers.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetCallers.Response, SharedProblemDetails>> Handle(GetCallers.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetCallers.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetCallersTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class GetCodeActionsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetCodeActions.Query, OneOf<GetCodeActions.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetCodeActions.Response, SharedProblemDetails>> Handle(GetCodeActions.Query query, CancellationToken cancellationToken) =>
		RunAsync<GetCodeActions.Response>(query, () =>
			Engine.Tool<GetCodeActionsTool>().HandleAsync(FromCaller(query), cancellationToken));
}

internal sealed class GetDiagnosticsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetDiagnostics.Query, OneOf<GetDiagnostics.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetDiagnostics.Response, SharedProblemDetails>> Handle(GetDiagnostics.Query query, CancellationToken cancellationToken) =>
		RunAsync<GetDiagnostics.Response>(query, () =>
			Engine.Tool<GetDiagnosticsTool>().HandleAsync(FromCaller(query), cancellationToken));
}

internal sealed class GetExpressionInfoHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetExpressionInfo.Query, OneOf<GetExpressionInfo.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetExpressionInfo.Response, SharedProblemDetails>> Handle(GetExpressionInfo.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetExpressionInfo.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetExpressionInfoTool>().HandleAsync(model, instance, FromCaller(query), token), cancellationToken);
}

internal sealed class GetMembersHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetMembers.Query, OneOf<GetMembers.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetMembers.Response, SharedProblemDetails>> Handle(GetMembers.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetMembers.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetMembersTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class GetSolutionStatusHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetSolutionStatus.Query, OneOf<GetSolutionStatus.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetSolutionStatus.Response, SharedProblemDetails>> Handle(GetSolutionStatus.Query query, CancellationToken cancellationToken) =>
		RunAsync<GetSolutionStatus.Response>(query, () =>
			Task.FromResult(Engine.Tool<GetSolutionStatusTool>().Handle(FromCaller(query))));
}

internal sealed class GetSymbolHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetSymbol.Query, OneOf<GetSymbol.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetSymbol.Response, SharedProblemDetails>> Handle(GetSymbol.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetSymbol.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetSymbolTool>().HandleAsync(model, instance, FromCaller(query), token), cancellationToken);
}

internal sealed class GetSymbolBodyHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetSymbolBody.Query, OneOf<GetSymbolBody.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetSymbolBody.Response, SharedProblemDetails>> Handle(GetSymbolBody.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetSymbolBody.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetSymbolBodyTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class GetTypeHierarchyHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<GetTypeHierarchy.Query, OneOf<GetTypeHierarchy.Response, SharedProblemDetails>>
{
	public Task<OneOf<GetTypeHierarchy.Response, SharedProblemDetails>> Handle(GetTypeHierarchy.Query query, CancellationToken cancellationToken) =>
		ReadAsync<GetTypeHierarchy.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<GetTypeHierarchyTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}

internal sealed class MultiQueryHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<MultiQuery.Query, OneOf<MultiQuery.Response, SharedProblemDetails>>
{
	public Task<OneOf<MultiQuery.Response, SharedProblemDetails>> Handle(MultiQuery.Query query, CancellationToken cancellationToken) =>
		RunAsync<MultiQuery.Response>(query, () =>
			Engine.Tool<MultiQueryTool>().HandleAsync(FromCaller(query), cancellationToken), fitToBudget: false);
}

internal sealed class OpenSolutionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<OpenSolution.Query, OneOf<OpenSolution.Response, SharedProblemDetails>>
{
	public Task<OneOf<OpenSolution.Response, SharedProblemDetails>> Handle(OpenSolution.Query query, CancellationToken cancellationToken) =>
		RunAsync<OpenSolution.Response>(query, () =>
			Engine.Tool<OpenSolutionTool>().HandleAsync(FromCaller(query), cancellationToken));
}

internal sealed class ReloadSolutionHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<ReloadSolution.Query, OneOf<ReloadSolution.Response, SharedProblemDetails>>
{
	public Task<OneOf<ReloadSolution.Response, SharedProblemDetails>> Handle(ReloadSolution.Query query, CancellationToken cancellationToken) =>
		RunAsync<ReloadSolution.Response>(query, () =>
			Task.FromResult(Engine.Tool<ReloadSolutionTool>().Handle(FromCaller(query))));
}

internal sealed class RemoveUnusedUsingsHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<RemoveUnusedUsings.Command, OneOf<RemoveUnusedUsings.Response, SharedProblemDetails>>
{
	public Task<OneOf<RemoveUnusedUsings.Response, SharedProblemDetails>> Handle(RemoveUnusedUsings.Command command, CancellationToken cancellationToken) =>
		RunAsync<RemoveUnusedUsings.Response>(command, () =>
			Engine.Tool<RemoveUnusedUsingsTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class RenameParameterHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<RenameParameter.Command, OneOf<RenameParameter.Response, SharedProblemDetails>>
{
	public Task<OneOf<RenameParameter.Response, SharedProblemDetails>> Handle(RenameParameter.Command command, CancellationToken cancellationToken) =>
		RunAsync<RenameParameter.Response>(command, () =>
			Engine.Tool<RenameParameterTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class RenameSymbolHandler(RoslynkEngine engine)
	: OperationHandler(engine), ICommandHandler<RenameSymbol.Command, OneOf<RenameSymbol.Response, SharedProblemDetails>>
{
	public Task<OneOf<RenameSymbol.Response, SharedProblemDetails>> Handle(RenameSymbol.Command command, CancellationToken cancellationToken) =>
		RunAsync<RenameSymbol.Response>(command, () =>
			Engine.Tool<RenameSymbolTool>().HandleAsync(FromCaller(command), cancellationToken));
}

internal sealed class SearchSymbolsHandler(RoslynkEngine engine)
	: OperationHandler(engine), IQueryHandler<SearchSymbols.Query, OneOf<SearchSymbols.Response, SharedProblemDetails>>
{
	public Task<OneOf<SearchSymbols.Response, SharedProblemDetails>> Handle(SearchSymbols.Query query, CancellationToken cancellationToken) =>
		ReadAsync<SearchSymbols.Response>(query, query.solutionId, (model, instance, budget, token) =>
			Engine.Tool<SearchSymbolsTool>().HandleAsync(model, instance, FromCaller(query), budget, token), cancellationToken);
}
