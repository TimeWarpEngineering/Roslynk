using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.MultiQuery;

public static partial class MultiQuery
{
	[Operation("multi_query", Description = """
		Runs independent read-only operations against one snapshot, returning results in request order.
		Operations cannot reference each other's output. Batch only queries needed for the question.

		operations.tool uses the schema enum; arguments use that tool's single-call names/defaults.
		Unknown/misspelled arguments are rejected in their own slot as UnknownArgument; the rest still
		run. A malformed operation (unknown tool, non-object arguments, missing tool) fails the whole
		call as Invalid at binding.

		All operations run. budget=<n> is the per-operation allowance; whole results are selected
		smallest-first until the response budget fills. Slots are complete or absent. Missing results
		report retry=<index>:<size> in request order; size is result cost/budget, rounded up to 0.1.
		- size >= 1.0: resend alone.
		- Sizes totaling <= 1.0: fit one continuation batch.
		- Sizes below 0.1: about ten per continuation batch is safe.

		Resend retry operations with original arguments from your request copy; frames contain only
		index/tool. Pass snapshot=<id> as expectSnapshot; changed generations return Stale with both ids:
		rerun the whole batch. Ratios are snapshot-specific; reruns remeasure/reselect.
		Only slot-body nextStartItem drives paging; retry means an omitted result, not a page.
		""", Group = "batch")]
	[DisplayName("Run several read-only queries in one call")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Read-only tools and arguments, in request order.
		""")]
		public required IReadOnlyList<MultiQueryOperation> operations { get; set; }

		[Description("""
		Previous snapshot id; mismatch returns Stale before execution.
		""")]
		[DefaultValue(null)]
		public string? expectSnapshot { get; set; } = null;
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
