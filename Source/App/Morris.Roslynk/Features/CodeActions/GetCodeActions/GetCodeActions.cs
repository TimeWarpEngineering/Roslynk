using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.CodeActions.GetCodeActions;

public static partial class GetCodeActions
{
	[Operation("get_code_actions", Description = """
		Lists fixes/refactorings at a source position or selection. Fixes use compiler/analyzer
		diagnostics at the span; refactorings use the span itself.

		Returns up to 50 <actionId>,<kind>,<diagnosticId> <title> lines; diagnosticId='-' for refactorings.
		Unknown documentPath returns NotFound.
		Razor offers compiler fixes/refactorings; positions outside C# return NotSupported.
		""", Group = "solution")]
	[DisplayName("List code actions")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.DocumentPath)]
		public required string documentPath { get; set; }

		[Description("""
		Position line.
		""")]
		public required int line { get; set; }

		[Description("""
		Position column.
		""")]
		public required int column { get; set; }

		[Description("""
		Selection end line.
		""")]
		[DefaultValue(null)]
		public int? endLine { get; set; } = null;

		[Description("""
		Selection end column.
		""")]
		[DefaultValue(null)]
		public int? endColumn { get; set; } = null;
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
