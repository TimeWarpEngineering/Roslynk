using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Results;
using Morris.Roslynk.Infrastructure.Workspaces;
using Morris.Roslynk.Infrastructure.Writing;
using TimeWarp.Foundation.Types;

namespace Morris.Roslynk.Infrastructure.Operations;

/// <summary>
/// Maps a tool <see cref="Error"/> or a <see cref="StaleWriteException"/> onto a
/// <see cref="SharedProblemDetails"/> and back onto the outline every tool already returns.
/// </summary>
internal static class Problems
{
	public static SharedProblemDetails From(Error error, SolutionStatus status)
	{
		if (!ProblemOutline.TryParse(OutlineError.Format(error, status), out SharedProblemDetails? problem))
			throw new InvalidOperationException("OutlineError.Format did not produce an error outline.");

		return problem!;
	}

	/// <summary>
	/// The one Stale mapping write tools use when a <see cref="StaleWriteException"/> leaves the apply pipeline.
	/// The path is the solution-relative form the outline already shows.
	/// </summary>
	public static string FormatStale(StaleWriteException exception, string? solutionDirectory, SolutionStatus status)
	{
		string path = SolutionRelativePath.Of(solutionDirectory, exception.FilePath) ?? exception.FilePath;
		return ProblemOutline.Format(From(Error.Stale(exception.Message, [path]), status));
	}
}
