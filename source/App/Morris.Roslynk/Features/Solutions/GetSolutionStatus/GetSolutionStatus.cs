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
		Lists loaded solutions: <solutionId> (absolute path with forward slashes),<status>,<loaded>/<total>.
		loaded tracks Building progress; total='?' until known after initial loading.

		Ready solutions add indented loadDiagnostic=<message> for analyzer/generator load failures,
		project evaluation failures or missing SDKs, which may explain compiler errors.
		Other states omit messages. Up to 20 per solution, then loadDiagnosticsTruncated=<n>.

		Any state adds indented watchDiagnostic=<message> when the file watcher failed in a way a
		reload would not fix (e.g. the OS watch limit): changes on disk may then not be picked up.
		Up to 20 per solution, then watchDiagnosticsTruncated=<n>; beyond 100 the text is not kept.

		solutionId restricts the output to that one loaded solution (the id open_solution echoed, or
		its absolute path in either slash form); omit it to list every loaded solution. An id naming
		no loaded solution returns NotFound with candidate=<solutionId> lines.
		""", Group = "solution", Title = "Get loaded-solution status")]
	[Destructive(false)]
	[OpenWorld(false)]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description("""
		Include all load and watch diagnostics.
		""")]
		[DefaultValue(false)]
		public bool allLoadDiagnostics { get; set; } = false;

		[Description("""
		Optional: restrict the output to one loaded solution. Null lists every loaded solution.
		""")]
		[DefaultValue(null)]
		public string? solutionId { get; set; } = null;
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
