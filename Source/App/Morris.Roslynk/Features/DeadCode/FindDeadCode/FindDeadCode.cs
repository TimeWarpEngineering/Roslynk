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

		maxResults caps the scan: count is candidates found; truncated=Y may mean the cap stopped scanning.
		Omit maxResults for a complete scan.
		""", Group = "references")]
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
		[DefaultValue(null)]
		public int? maxResults { get; set; } = null;

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
