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
		filePath accepts absolute, solution-relative, or a unique path suffix/file name; a fallback match
		adds resolvedPath=<solution-relative path>. Unknown filePath returns NotFound; several matching
		files return Ambiguous with those paths as candidates.
		Razor offers compiler fixes/refactorings; positions outside C# return NotSupported.
		""", Group = "solution")]
	[DisplayName("List code actions")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.FilePath)]
		public required string filePath { get; set; }

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

	public sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
