using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.References.FindReads;

public static partial class FindReads
{
	[Operation("find_reads", Description = """
		SupportsPaging=Y

		Finds reads, including compound/increment/ref, using the shared access contract.
		Body follows find_references with one location per leaf.
		""", Group = "references")]
	[DisplayName("Find reads of a field, property or parameter")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(OutlineDescriptions.AccessSymbolName)]
		public required string symbolName { get; set; }

		[Description("""
		Location cap.
		""")]
		[DefaultValue(null)]
		public int? maxResults { get; set; } = null;

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

	public sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
