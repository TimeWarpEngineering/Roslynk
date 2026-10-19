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
using TimeWarp.Terminal;

namespace Morris.Roslynk.Cli.Forwarding;

// One closed forwarding handler per [Operation], so the generated mediator registers a handler for every
// route. A new operation needs its line here as well as its file in the compile list (see AGENTS.md).

internal sealed class ApplyCodeActionHandler(ITerminal terminal) : ForwardingCommandHandler<ApplyCodeAction.Command, ApplyCodeAction.Response>(terminal);

internal sealed class ApplyCodeFixHandler(ITerminal terminal) : ForwardingCommandHandler<ApplyCodeFix.Command, ApplyCodeFix.Response>(terminal);

internal sealed class ApplyPatchHandler(ITerminal terminal) : ForwardingCommandHandler<ApplyPatch.Command, ApplyPatch.Response>(terminal);

internal sealed class ChangeSignatureHandler(ITerminal terminal) : ForwardingCommandHandler<ChangeSignature.Command, ChangeSignature.Response>(terminal);

internal sealed class ExtractMethodHandler(ITerminal terminal) : ForwardingCommandHandler<ExtractMethod.Command, ExtractMethod.Response>(terminal);

internal sealed class FindDeadCodeHandler(ITerminal terminal) : ForwardingQueryHandler<FindDeadCode.Query, FindDeadCode.Response>(terminal);

internal sealed class FindDeadConditionalsHandler(ITerminal terminal) : ForwardingQueryHandler<FindDeadConditionals.Query, FindDeadConditionals.Response>(terminal);

internal sealed class FindDefinitionHandler(ITerminal terminal) : ForwardingQueryHandler<FindDefinition.Query, FindDefinition.Response>(terminal);

internal sealed class FindImplementationsHandler(ITerminal terminal) : ForwardingQueryHandler<FindImplementations.Query, FindImplementations.Response>(terminal);

internal sealed class FindReadsHandler(ITerminal terminal) : ForwardingQueryHandler<FindReads.Query, FindReads.Response>(terminal);

internal sealed class FindReferencesHandler(ITerminal terminal) : ForwardingQueryHandler<FindReferences.Query, FindReferences.Response>(terminal);

internal sealed class FindWritesHandler(ITerminal terminal) : ForwardingQueryHandler<FindWrites.Query, FindWrites.Response>(terminal);

internal sealed class GetCalleesHandler(ITerminal terminal) : ForwardingQueryHandler<GetCallees.Query, GetCallees.Response>(terminal);

internal sealed class GetCallersHandler(ITerminal terminal) : ForwardingQueryHandler<GetCallers.Query, GetCallers.Response>(terminal);

internal sealed class GetCodeActionsHandler(ITerminal terminal) : ForwardingQueryHandler<GetCodeActions.Query, GetCodeActions.Response>(terminal);

internal sealed class GetDiagnosticsHandler(ITerminal terminal) : ForwardingQueryHandler<GetDiagnostics.Query, GetDiagnostics.Response>(terminal);

internal sealed class GetExpressionInfoHandler(ITerminal terminal) : ForwardingQueryHandler<GetExpressionInfo.Query, GetExpressionInfo.Response>(terminal);

internal sealed class GetMembersHandler(ITerminal terminal) : ForwardingQueryHandler<GetMembers.Query, GetMembers.Response>(terminal);

internal sealed class GetSolutionStatusHandler(ITerminal terminal) : ForwardingQueryHandler<GetSolutionStatus.Query, GetSolutionStatus.Response>(terminal);

internal sealed class GetSymbolHandler(ITerminal terminal) : ForwardingQueryHandler<GetSymbol.Query, GetSymbol.Response>(terminal);

internal sealed class GetSymbolBodyHandler(ITerminal terminal) : ForwardingQueryHandler<GetSymbolBody.Query, GetSymbolBody.Response>(terminal);

internal sealed class GetTypeHierarchyHandler(ITerminal terminal) : ForwardingQueryHandler<GetTypeHierarchy.Query, GetTypeHierarchy.Response>(terminal);

internal sealed class MultiQueryHandler(ITerminal terminal) : ForwardingQueryHandler<MultiQuery.Query, MultiQuery.Response>(terminal);

internal sealed class OpenSolutionHandler(ITerminal terminal) : ForwardingQueryHandler<OpenSolution.Query, OpenSolution.Response>(terminal);

internal sealed class ReloadSolutionHandler(ITerminal terminal) : ForwardingQueryHandler<ReloadSolution.Query, ReloadSolution.Response>(terminal);

internal sealed class RemoveUnusedUsingsHandler(ITerminal terminal) : ForwardingCommandHandler<RemoveUnusedUsings.Command, RemoveUnusedUsings.Response>(terminal);

internal sealed class RenameParameterHandler(ITerminal terminal) : ForwardingCommandHandler<RenameParameter.Command, RenameParameter.Response>(terminal);

internal sealed class RenameSymbolHandler(ITerminal terminal) : ForwardingCommandHandler<RenameSymbol.Command, RenameSymbol.Response>(terminal);

internal sealed class SearchSymbolsHandler(ITerminal terminal) : ForwardingQueryHandler<SearchSymbols.Query, SearchSymbols.Response>(terminal);
