using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.CodeActions.ApplyCodeFix;

public static partial class ApplyCodeFix
{
	[Operation("apply_code_fix", Description = """
		Fixes the diagnostic containing the supplied position, using its id and location from get_diagnostics.

		Multiple fixes return Conflict with candidate=<actionId>,Fix,<diagnosticId> <title>:
		choose by title and pass actionId to apply_code_action; do not repeat apply_code_fix.

		Errors: NotFound for an unloaded document or no diagnostic at the position; Invalid for
		line/column below 1; NotSupported for no registered fix; Conflict for no changes (without candidates).
		CS8019/IDE0005 fixes remove the @using on that line.

		Additional header: action.
		""", Group = "edit")]
	[DisplayName("Apply a diagnostic's fix")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.DocumentPath)]
		public required string documentPath { get; set; }

		[Description("""
		Diagnostic id, e.g. CS0219/IDE0005.
		""")]
		public required string diagnosticId { get; set; }

		[Description("""
		Diagnostic line from get_diagnostics.
		""")]
		public required int line { get; set; }

		[Description("""
		Diagnostic column from get_diagnostics.
		""")]
		public required int column { get; set; }

		[Description(ParameterDescriptions.CheckOnly)]
		[DefaultValue(false)]
		public bool checkOnly { get; set; } = false;
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
