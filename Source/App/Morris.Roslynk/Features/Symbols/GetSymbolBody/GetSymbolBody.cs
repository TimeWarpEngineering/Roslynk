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
		local functions or types. Razor returns generated C#.
		Single declaration: project/path/loc headers, then source.
		Partial types/methods: parts=<n>, then blocks headed part=<n>,project=...,path=...,loc=...,lines=<count>.
		Includes generated implementations (GeneratedRegex, LoggerMessage, LibraryImport).

		Ambiguous names/overloads return candidates. Namespaces/metadata-only symbols: NotSupported.
		Metadata spellings of source types (Ns.Box`1, N.Outer+Inner) return source.

		Paging: startItem counts returned lines (including requested leading trivia), not file lines.
		Past-end startItem returns Invalid with count.
		Paging requires one part; multiple parts without part= returns Invalid.
		Oversized parts omit text with omitted=Y, truncated=Y, omittedParts=<k>; fetch with part=<n>, then page.
		""", Group = "symbols")]
	[DisplayName("Get symbol source text")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Symbol name.
		""")]
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
	}

	public sealed class Response : IOutlineResponse
	{
		public string Text { get; set; } = "";
	}

	internal sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
