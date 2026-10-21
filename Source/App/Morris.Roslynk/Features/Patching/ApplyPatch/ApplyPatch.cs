using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Patching.ApplyPatch;

public static partial class ApplyPatch
{
	[Operation("apply_patch", Description = """
		Applies content-anchored git unified diffs to existing text files. Targets: solution-compiled
		.cs/.razor/.cshtml (model synchronized), plus other text files under the solution folder
		(e.g. .csproj/.json/.md). Creation/deletion, binary files and paths outside that folder:
		NotSupported; missing files: NotFound.

		Header-only result: applied=<Y|N>. Hunk headers may omit line numbers (bare '@@'); each hunk
		must match exactly once, so provide unique context. Unique-suffix paths also resolve.
		Ambiguous paths return candidate=<path>; rejections list rejected=<path>.
		Unmatched/ambiguous hunks: Conflict with file/reason; no file sections: Invalid.

		baseVersions pairs patch paths with documentVersion: SHA-256 hex of loaded text encoded as
		UTF-8 without BOM. No read tool reports this hash; supply only if you can compute it exactly.
		Mismatches return Stale with stale=<path>.
		""", Group = "edit")]
	[DisplayName("Apply a patch")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Git unified diff (---/+++/@@).
		""")]
		public required string patch { get; set; }

		[Description("""
		Patch paths and expected documentVersion hashes.
		""")]
		[DefaultValue(null)]
		public IReadOnlyList<FileVersion>? baseVersions { get; set; } = null;

		[Description(ParameterDescriptions.CheckOnly)]
		[DefaultValue(false)]
		public bool checkOnly { get; set; } = false;
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
