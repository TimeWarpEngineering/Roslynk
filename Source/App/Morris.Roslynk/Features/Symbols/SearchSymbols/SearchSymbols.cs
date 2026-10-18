using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.SearchSymbols;

public static partial class SearchSymbols
{
	[Operation("search_symbols", Description = """
		SupportsPaging=Y

		Searches source symbol names, including generated declarations
		and local functions nested under their declaring member/outer local functions.
		Body: project > folder > file > namespace > type > member. Members nest under parent types;
		type locations appear only when the type itself matches.
		""")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Case-insensitive name substring.
		""")]
		public required string query { get; set; }

		[Description("""
		Result cap.
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

	internal sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
