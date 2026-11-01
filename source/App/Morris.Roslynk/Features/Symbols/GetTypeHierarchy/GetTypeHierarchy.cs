using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.GetTypeHierarchy;

public static partial class GetTypeHierarchy
{
	[Operation("get_type_hierarchy", Description = """
		SupportsPaging=Y

		Returns base types (including metadata), implemented interfaces and known derived types.
		Header: resolvedType. Body sections: base, interfaces, derived (empty sections omitted).
		Entries: <typeKind>,<fully-qualified name>; typeKind=class|struct|interface|enum|delegate.
		symbolName accepts a uniquely-matching dotted-segment suffix.
		""" + "\n\n" + OutlineDescriptions.CountOnlyOutput, Group = "symbols", Title = "Get a type's hierarchy")]
	[Destructive(false)]
	[OpenWorld(false)]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.SymbolName)]
		public required string symbolName { get; set; }

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
