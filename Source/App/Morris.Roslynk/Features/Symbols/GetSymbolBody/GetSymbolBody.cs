using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.GetSymbolBody;

public static partial class GetSymbolBody
{
	[Operation("get_symbol_body", Description = """
		SupportsPaging=Y

		Returns verbatim declarations of methods, constructors, properties, indexers, events, fields,
		local functions or types. Razor declarations map back to their .razor/.cshtml source: @code
		members, @inject properties (the directive line) and BuildRenderTree (its mapped markup lines).
		A Razor-generated declaration with no #line mapping into Razor source returns generated C#.
		Single declaration: project/path/loc headers, then source.
		Partial types/methods: parts=<n>, then blocks headed part=<n>,project=...,path=...,loc=...,lines=<count>.
		Includes generated implementations (GeneratedRegex, LoggerMessage, LibraryImport).

		Ambiguous names/overloads return candidates. Namespaces/metadata-only symbols: NotSupported.
		Metadata spellings of source types (Ns.Box`1, N.Outer+Inner) return source.
		A partially qualified symbolName resolves when exactly one symbol matches it as a dotted-segment
		suffix; the result then carries resolvedSymbol=<canonical fully qualified signature>.

		Paging: startItem counts returned lines (including requested leading trivia), not file lines.
		Past-end startItem returns Invalid with count.
		Paging requires one part; multiple parts without part= returns Invalid.
		Oversized parts omit text with omitted=Y, truncated=Y, omittedParts=<k>; fetch with part=<n>, then page.

		countOnly=true returns the headers plus count=<n> (returned-text lines; every part's lines when
		part=0 spans several parts) and no text; startItem and maxSize are ignored.
		""", Group = "symbols")]
	[DisplayName("Get symbol source text")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.SymbolName)]
		public required string symbolName { get; set; }

		[Description("""
		Include leading comments, XML docs and directives.
		""")]
		[DefaultValue(false)]
		public bool includeLeadingTrivia { get; set; } = false;

		[Description("""
		First returned-text line (1-based).
		""")]
		[DefaultValue(1)]
		public int startItem { get; set; } = 1;

		[Description(ParameterDescriptions.MaxSize)]
		[DefaultValue(0)]
		public int maxSize { get; set; } = 0;

		[Description("""
		Declaration part (1-based); 0 returns all, omitting oversized parts.
		""")]
		[DefaultValue(0)]
		public int part { get; set; } = 0;

		[Description("""
		Return counts only: count=<returned-text lines>, no body and no paging headers; startItem and maxSize are ignored.
		""")]
		[DefaultValue(false)]
		public bool countOnly { get; set; } = false;
	}

	public sealed class Response : IOutlineResponse
	{
		public string Text { get; set; } = "";
	}

	public sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
