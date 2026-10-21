using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.GetSymbol;

public static partial class GetSymbol
{
	[Operation("get_symbol", Description = """
		Returns a symbol's declaration without implementation. Source results: project, path and loc
		headers, then the verbatim declaration before its opening brace or '=>'.
		Without source: source=metadata, kind, signature and assembly headers.
		""", Group = "symbols")]
	[DisplayName("Get symbol details")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Symbol name.
		""")]
		public required string symbolName { get; set; }
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
