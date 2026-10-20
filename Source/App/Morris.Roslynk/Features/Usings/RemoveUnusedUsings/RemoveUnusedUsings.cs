using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Usings.RemoveUnusedUsings;

public static partial class RemoveUnusedUsings
{
	[Operation("remove_unused_usings", Description = """
		Removes unnecessary using directives (CS8019), solution-wide or in documentPath, including
		Razor @using lines. Additional header: removedCount.
		No changes: applied=N, removedCount=0; safe to rerun.
		NotFound: unloaded source file.
		""", Group = "edit")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Source file to clean; omit for the whole solution.
		""")]
		[DefaultValue(null)]
		public string? documentPath { get; set; } = null;

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
