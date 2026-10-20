using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Solutions.GetSolutionStatus;

public static partial class GetSolutionStatus
{
	[Operation("get_solution_status", Description = """
		Lists loaded solutions: <solutionId>,<status>,<loaded>/<total>.
		loaded tracks Building progress; total='?' until known after initial loading.

		Ready solutions add indented loadDiagnostic=<message> for analyzer/generator load failures,
		project evaluation failures or missing SDKs, which may explain compiler errors.
		Other states omit messages. Up to 20 per solution, then loadDiagnosticsTruncated=<n>.
		""", Group = "solution")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description("""
		Include all load diagnostics.
		""")]
		[DefaultValue(false)]
		public bool allLoadDiagnostics { get; set; } = false;
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
