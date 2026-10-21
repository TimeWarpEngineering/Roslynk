using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.FindDefinition;

public static partial class FindDefinition
{
	[Operation("find_definition", Description = """
		Resolves the symbol at a source position. Returns fullName (including method/indexer parameter
		types) and kind; source symbols add project, path and loc, metadata symbols add assembly.
		No binding in any projection returns NotFound.
		""", Group = "symbols")]
	[DisplayName("Find a symbol's definition")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Usage's .cs file path.
		""")]
		public required string filePath { get; set; }

		[Description("""
		Usage line.
		""")]
		public required int line { get; set; }

		[Description("""
		Usage column.
		""")]
		public required int column { get; set; }
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
