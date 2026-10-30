using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.SearchSymbols;

public static partial class SearchSymbols
{
	[Operation("search_symbols", Description = """
		SupportsPaging=Y

		Searches source symbol names, including generated declarations
		and local functions nested under their declaring member/outer local functions.
		symbolNameFilter matches a symbol's own name, not free text and not its members: a bare name is a
		case-insensitive substring, and a dotted name is container-qualified - the last segment matches a
		symbol name and the preceding segments must be a whole-segment suffix of its containing
		type/namespace chain (Widget.Compute, SimpleLibrary.Widget.Compute). To list a type's members, use
		get_members; a member cannot be selected by its container alone here.
		Body: project > folder > file > namespace > type > member. Members nest under parent types;
		type locations appear only when the type itself matches.
		'kinds' filters the matched declarations by outline kind (namespace, class, struct, interface, enum,
		delegate, module, method, property, field, event, localfunction), case-insensitively; an unknown kind
		name is error=Invalid rather than an empty result.
		""" + "\n\n" + OutlineDescriptions.CountOnlyOutput, Group = "symbols", Title = "Search symbols by name")]
	[Destructive(false)]
	[OpenWorld(false)]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.SymbolNameFilter)]
		public required string symbolNameFilter { get; set; }

		[Description("""
		Optional kind filter: a JSON array of kind names, e.g. ["Method"]. A JSON-array string such as "[\"Method\"]" is also accepted. Kinds (case-insensitive): namespace, class, struct, interface, enum, delegate, module, method, property, field, event, localfunction. Omit or pass [] for all kinds.
		""")]
		[DefaultValue(null)]
		public string[]? kinds { get; set; } = null;

		[Description(ParameterDescriptions.StartItem)]
		[DefaultValue(1)]
		public int startItem { get; set; } = 1;

		[Description(ParameterDescriptions.MaxSize)]
		[DefaultValue(0)]
		public int maxSize { get; set; } = 0;

		[Description(ParameterDescriptions.CountOnly)]
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
