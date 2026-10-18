using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Operations;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Results;
using Morris.Roslynk.Infrastructure.Writing;
using TimeWarp.Foundation.Types;

namespace Morris.Roslynk.Tests.Infrastructure.Operations;

public class ProblemOutlineTests
{
	[Fact]
	public void WhenOutlineErrorFormatsAnAmbiguousResult_ThenTheProblemRoundTrips()
	{
		Error error = Error.Ambiguous("pick one", ["N.A", "N.B"]);
		string text = OutlineError.Format(error, SolutionStatus.Building);

		Assert.True(ProblemOutline.TryParse(text, out SharedProblemDetails? problem));
		Assert.Equal("Ambiguous", problem!.Type);
		Assert.Equal("pick one", problem.Detail);
		Assert.Equal(text, ProblemOutline.Format(problem));
		Assert.Equal(text, ProblemOutline.Format(Problems.From(error, SolutionStatus.Building)));
	}

	[Fact]
	public void WhenTheOutlineCarriesRejectedBeforeCandidates_ThenTheOrderSurvives()
	{
		string text = "error=Invalid\nerrorMessage=rejected hunk\nrejected=file.cs\ncandidate=other.cs\nstatus=Updating\n";

		Assert.True(ProblemOutline.TryParse(text, out SharedProblemDetails? problem));
		Assert.Equal(text, ProblemOutline.Format(problem!));
	}

	[Fact]
	public void WhenASnapshotHeaderFollowsTheError_ThenItRoundTrips()
	{
		string text = "error=Stale\nerrorMessage=the solution changed\nstale=src/A.cs\nsnapshot=abc\n";

		Assert.True(ProblemOutline.TryParse(text, out SharedProblemDetails? problem));
		Assert.Equal("Stale", problem!.Type);
		Assert.Equal(text, ProblemOutline.Format(problem));
	}

	[Fact]
	public void WhenABodyFollowsTheHeaders_ThenTheBodyRoundTrips()
	{
		string text = "error=Invalid\nerrorMessage=no\n\nkept body\n";

		Assert.True(ProblemOutline.TryParse(text, out SharedProblemDetails? problem));
		Assert.Equal(text, ProblemOutline.Format(problem!));
	}

	[Fact]
	public void WhenTheTextIsASuccessOutline_ThenItIsNotAProblem()
	{
		Assert.False(ProblemOutline.TryParse("operations=1\n", out _));
	}

	[Fact]
	public void WhenAStaleWriteIsMapped_ThenTheOutlineIsTheSharedStaleShape()
	{
		var exception = new StaleWriteException("/work/src/File.cs");
		string text = Problems.FormatStale(exception, "/work", SolutionStatus.Ready);
		string direct = OutlineError.Format(
			Error.Stale(exception.Message, ["src/File.cs"]),
			SolutionStatus.Ready);

		Assert.Equal(direct, text);
		Assert.DoesNotContain("status=", text, StringComparison.Ordinal);
		Assert.Contains("error=Stale\n", text, StringComparison.Ordinal);
		Assert.Contains("stale=src/File.cs\n", text, StringComparison.Ordinal);
	}
}
