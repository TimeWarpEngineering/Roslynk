using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Diagnostics.GetDiagnostics;

public static partial class GetDiagnostics
{
	[Operation("get_diagnostics", Description = """
		SupportsPaging=Y

		Returns compiler/analyzer diagnostics.

		errors/warnings/infos/hidden count all content-filtered severities regardless of include flags.
		The body lists flagged severities; summaryOnly summarizes all if none are flagged, otherwise
		only flagged ones. Details: project > folder > file > severity; leaves: <id>,<line:col>,<message>.
		Summary: <id>,<count>,<project>,<path>,<line:col>,<message> per id, ordered by count descending then id.

		projectName/filePath/ids filter after compilation, reusing cached results; active filters echo
		filter=<parameter>:<value>. Empty filtered results do not prove the solution clean.
		Unknown project/file returns NotFound with candidates. Known matches without diagnostics return zeros.
		Private fixer-trigger IDs are omitted; fix the public rule instead (e.g. IDE0005).
		""", Group = "solution")]
	[DisplayName("Get compiler diagnostics")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		List errors.
		""")]
		[DefaultValue(false)]
		public bool includeErrors { get; set; } = false;

		[Description("""
		List warnings.
		""")]
		[DefaultValue(false)]
		public bool includeWarnings { get; set; } = false;

		[Description("""
		List info diagnostics.
		""")]
		[DefaultValue(false)]
		public bool includeInfo { get; set; } = false;

		[Description("""
		List hidden diagnostics.
		""")]
		[DefaultValue(false)]
		public bool includeHidden { get; set; } = false;

		[Description("""
		Run analyzers; false is faster, compiler-only.
		""")]
		[DefaultValue(true)]
		public bool includeAnalyzers { get; set; } = true;

		[Description("""
		Project filter: outline name with optional extension, case-insensitive; all loaded target frameworks.
		""")]
		[DefaultValue(null)]
		public string? projectName { get; set; } = null;

		[Description("""
		File filter (case-insensitive), including diagnostics mapped to Razor source.
		""")]
		[DefaultValue(null)]
		public string? filePath { get; set; } = null;

		[Description("""
		Diagnostic ids (case-insensitive), e.g. ["CS0246"]; null/empty matches all.
		""")]
		[DefaultValue(null)]
		public string[]? ids { get; set; } = null;

		[Description("""
		Body-entry cap (ids with summaryOnly). Capped bodies report count=<listed>, truncated=Y.
		""")]
		[DefaultValue(null)]
		public int? maxResults { get; set; } = null;

		[Description("""
		Summarize by diagnostic id.
		""")]
		[DefaultValue(false)]
		public bool summaryOnly { get; set; } = false;

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

	public sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
