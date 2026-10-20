using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.GetMembers;

public static partial class GetMembers
{
	[Operation("get_members", Description = """
		SupportsPaging=Y

		Lists methods/properties/fields/events/nested types grouped by declaring file ('<metadata>' for
		referenced assemblies). Leaves: <memberKind>,<name>,<loc>, plus pipe-delimited parameter types
		for methods with parameters. Metadata loc is empty. Local functions nest under their member
		in declaration order, one level per nesting.

		Includes all accessibilities. Filter with nameFilter/include*; use get_symbol_body for bodies.
		""", Group = "symbols")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Type name.
		""")]
		public required string typeName { get; set; }

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
		Include methods.
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
