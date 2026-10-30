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
		Removes unnecessary using directives (CS8019), solution-wide or in filePath, including
		Razor @using lines. filePath also accepts a unique path suffix or file name; a fallback match
		adds resolvedPath=<solution-relative path>. Additional header: removedCount.
		No changes: applied=N, removedCount=0; safe to rerun.
		NotFound: unloaded source file. Ambiguous: several matching files, with those paths as candidates.
		""", Group = "edit", Title = "Remove unused usings")]
	[Destructive]
	[OpenWorld(false)]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Source file to clean; omit for the whole solution.
		""")]
		[DefaultValue(null)]
		public string? filePath { get; set; } = null;

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
