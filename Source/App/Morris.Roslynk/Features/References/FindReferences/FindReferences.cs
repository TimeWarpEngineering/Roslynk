using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.References.FindReferences;

public static partial class FindReferences
{
	[Operation("find_references", Description = """
		SupportsPaging=Y

		Finds semantic references, including partial classes/code-behind.
		Body: project > folder > file > namespace > type > member. Shared declarations appear once;
		type locations appear only when the declaration references the target; collapsed references
		use pipe-delimited loc lists.

		Combine with get_callers/get_callees/find_implementations/get_type_hierarchy via multi_query for
		usage/impact analysis.
		symbolName accepts a uniquely-matching dotted-segment suffix.
		""" + "\n\n" + OutlineDescriptions.CountOnlyOutput, Group = "references")]
	[DisplayName("Find references to a symbol")]
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
