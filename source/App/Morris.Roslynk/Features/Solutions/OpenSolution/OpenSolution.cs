using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Solutions.OpenSolution;

public static partial class OpenSolution
{
	[Operation("open_solution", Description = """
		Opens a .sln/.slnx and waits for the initial load (up to the server's load timeout); repeated opens reuse the instance.
		Headers: solutionId (absolute path with forward slashes, reusable verbatim in JSON), status, projects, loadDiagnostics. Body: <projectPath>,<documentCount> per project.
		noWait=true starts or reuses the load and returns immediately with status=Building (fire-and-forget pre-warming).
		Missing solution: NotFound; failed load: Faulted.
		""", Group = "solution")]
	[DisplayName("Open a C# solution")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionPath)]
		public required string solutionPath { get; set; }

		[Description("""
		Return immediately without waiting for the initial load.
		""")]
		[DefaultValue(false)]
		public bool noWait { get; set; } = false;
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
