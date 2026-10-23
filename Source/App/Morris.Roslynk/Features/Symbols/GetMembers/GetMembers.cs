using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.GetMembers;

public static partial class GetMembers
{
	[Operation("get_members", Description = """
		SupportsPaging=Y

		Lists methods/properties/fields/events/nested types grouped by declaring file ('<metadata>' for
		referenced assemblies). Razor-generated members are placed under their .razor/.cshtml file with
		the mapped location (@inject directive lines included). Leaves: <memberKind>,<name>,<loc>, plus
		pipe-delimited parameter types for methods with parameters. Metadata loc is empty. Local functions
		nest under their member in declaration order, one level per nesting. Property/event accessors are
		not listed as methods: each property/event appears once, as its own leaf, and includeMethods covers
		ordinary methods, constructors and local functions only.

		Includes all accessibilities. Filter with nameFilter/include*; use get_symbol_body for bodies.
		symbolName accepts a uniquely-matching dotted-segment suffix.
		""" + "\n\n" + OutlineDescriptions.CountOnlyOutput, Group = "symbols")]
	[DisplayName("Get a type's members")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.SymbolName)]
		public required string symbolName { get; set; }

		[Description("""
		Include inherited members.
		""")]
		[DefaultValue(false)]
		public bool includeInherited { get; set; } = false;

		[Description("""
		Case-insensitive name filter: trailing '*' means prefix; otherwise substring. Null matches all.
		""")]
		[DefaultValue(null)]
		public string? nameFilter { get; set; } = null;

		[Description("""
		Include methods (ordinary methods and constructors; property/event accessors are represented by their property/event leaf).
		""")]
		[DefaultValue(true)]
		public bool includeMethods { get; set; } = true;

		[Description("""
		Include fields.
		""")]
		[DefaultValue(true)]
		public bool includeFields { get; set; } = true;

		[Description("""
		Include properties.
		""")]
		[DefaultValue(true)]
		public bool includeProperties { get; set; } = true;

		[Description("""
		Include events.
		""")]
		[DefaultValue(true)]
		public bool includeEvents { get; set; } = true;

		[Description("""
		Include nested types.
		""")]
		[DefaultValue(true)]
		public bool includeNestedTypes { get; set; } = true;

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
