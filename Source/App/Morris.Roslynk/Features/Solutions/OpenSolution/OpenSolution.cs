using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Solutions.OpenSolution;

public static partial class OpenSolution
{
	[Operation("open_solution", Description = """
		Opens a .sln/.slnx asynchronously; repeated opens reuse the instance.
		Headers: solutionId, status, projects, loadDiagnostics. Body: <projectPath>,<documentCount> per project.
		While loading: status=Building, empty body. Missing solution: NotFound; failed load: Faulted.
		""", Group = "solution")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description("""
		Absolute path to the .sln or .slnx file to open.
		""")]
		public required string solutionPath { get; set; }
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
