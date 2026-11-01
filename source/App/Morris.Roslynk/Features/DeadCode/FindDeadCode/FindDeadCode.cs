using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.DeadCode.FindDeadCode;

public static partial class FindDeadCode
{
	[Operation("find_dead_code", Description = """
		SupportsPaging=Y

		Finds unreferenced types/methods/properties/fields/events, conservatively excluding interface
		implementations, virtual/override chains, tests, generated and DI/reflection-activated members.
		Excludes public APIs unless includePublic. find_dead_conditionals finds unreachable #if branches.

		Body: project > folder > file > namespace > type; leaves:
		<memberKind>,<memberName>,<loc>,<confidence>,<reason>, confidence=High|Medium.
		Dead types have their own leaf/location. loc spans the declaration for apply_patch removal.
		Unused overloads have separate locations, without parameter lists: get_symbol_body offers
		candidates. The host decides removals. Scan is per-symbol; narrow large solutions with scope.

		maxSize caps the scan: count is candidates found; truncated=Y may mean the cap stopped scanning.
		Omit maxSize for a complete scan.

		countOnly=true returns the headers plus count=<n> and no leaves; maxSize still caps the scan,
		so truncated=Y means the cap stopped the scan.
		""", Group = "references", Title = "Find unused code")]
	[Destructive(false)]
	[OpenWorld(false)]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Name prefix, e.g. MyApp.Services; omit for the whole solution.
		""")]
		[DefaultValue(null)]
		public string? scope { get; set; } = null;

		[Description("""
		Include unreferenced public/protected members (the API surface).
		""")]
		[DefaultValue(false)]
		public bool includePublic { get; set; } = false;

		[Description("""
		Scan cap; omit for a complete scan.
		""")]
		[DefaultValue(0)]
		public int maxSize { get; set; } = 0;

		[Description(ParameterDescriptions.StartItem)]
		[DefaultValue(1)]
		public int startItem { get; set; } = 1;

		[Description("""
		Return counts only: count= and no leaves, no body and no paging headers; startItem is ignored, while maxSize still caps the scan.
		""")]
		[DefaultValue(false)]
		public bool countOnly { get; set; } = false;
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
