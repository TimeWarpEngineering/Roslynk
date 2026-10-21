using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Solutions.ReloadSolution;

public static partial class ReloadSolution
{
	[Operation("reload_solution", Description = """
		Re-evaluates an open solution from disk using MSBuild. Additional reload reasons: missed watcher
		events (network/WSL/container filesystems), global.json changes or retrying a Faulted load.

		Returns immediately: solutionId, status, projects. Previous snapshot serves reads with
		status=Building until replacement is ready. Files are unchanged; failed reloads return Faulted.
		""", Group = "solution")]
	[DisplayName("Reload a solution")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }
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
