using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.CodeActions.ApplyCodeAction;

public static partial class ApplyCodeAction
{
	[Operation("apply_code_action", Description = """
		Applies actionId from get_code_actions, re-resolving at its original position; retains no action state.

		Errors: Conflict if the action no longer resolves (rerun get_code_actions); Invalid for a
		malformed actionId. Adding usings/members to Razor-generated
		scaffolding may be unmappable.

		Additional header: action.
		""", Group = "edit")]
	[DisplayName("Apply a code action")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		actionId from get_code_actions.
		""")]
		public required string actionId { get; set; }

		[Description(ParameterDescriptions.CheckOnly)]
		[DefaultValue(false)]
		public bool checkOnly { get; set; } = false;
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
