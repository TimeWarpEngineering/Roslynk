using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Conditionals.FindDeadConditionals;

public static partial class FindDeadConditionals
{
	[Operation("find_dead_conditionals", Description = """
		SupportsPaging=Y

		Finds #if/#elif/#else branches absent from every checked configuration: each project's symbols
		and those symbols without DEBUG, across loaded frameworks. Results are possible dead code;
		missing workloads or CI-only defines can cause false positives.

		Header: deadConditionals=<n>. Body grouped by file; leaves: <line:col>,<directive>,<condition>.
		directive=if|elif|else; condition is the raw expression or '(else)'.
		Use find_dead_code for unreferenced symbols.
		""", Group = "references")]
	[DisplayName("Find never-compiled #if branches")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.StartItem)]
		[DefaultValue(1)]
		public int startItem { get; set; } = 1;

		[Description(ParameterDescriptions.MaxSize)]
		[DefaultValue(0)]
		public int maxSize { get; set; } = 0;
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
