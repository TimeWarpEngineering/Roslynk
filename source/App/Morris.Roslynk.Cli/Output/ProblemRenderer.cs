using Morris.Roslynk.Infrastructure.Operations;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;

namespace Morris.Roslynk.Cli.Output;

/// <summary>
/// Renders a problem as the header-only <c>error=</c> outline the tool descriptions document. With
/// <c>UseVerbatimRendererOutput()</c> Nuru writes this text to stderr unchanged and exits with
/// <see cref="SharedProblemDetails.Status"/> (see <see cref="ExitCodes"/>); MCP returns it as the error result.
/// </summary>
internal sealed class ProblemRenderer : IResponseRenderer<SharedProblemDetails>
{
	public string Render(SharedProblemDetails response) => ProblemOutline.Format(response);
}
