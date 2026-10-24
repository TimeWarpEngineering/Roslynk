using System.Text.Json;
using Morris.Roslynk.Cli.Output;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Daemon;

namespace Morris.Roslynk.Cli.Hosting;

/// <summary>
/// The app's route forwarder: Nuru's daemon client, made strict. Every operation call goes to the served
/// daemon (<c>roslynk serve</c>), which is started when none answers, so CLI calls and MCP sessions share one
/// warm engine and the client never loads Roslyn. When no server of this build can be started, or the server
/// stops answering during the call, the call fails with one line and exit 69 (<see cref="ExitCodes.Unavailable"/>)
/// instead of loading the engine here. <c>--no-daemon</c> still runs a call in this process.
/// </summary>
/// <remarks>
/// Nuru's client runs a call locally when an automatic start fails, and reports a forced start's failure as exit
/// 1. Here every call is forced, and a failure is told apart from a call that ran by asking the socket
/// afterwards: a server of this build that answers ran the call (its exit 1 is a usage error), none means the
/// call got no answer.
/// </remarks>
internal sealed class RoslynkForwarder : INuruRouteForwarder
{
	private readonly NuruDaemonClient Client;

	public RoslynkForwarder(NuruDaemonClient client)
	{
		Client = client;
	}

	/// <summary>Replaces the forwarder <c>AddServeRoute()</c> attached to <paramref name="app"/>.</summary>
	public static void Attach(NuruApp app)
	{
		if (app.RouteForwarder is NuruDaemonClient client)
			app.RouteForwarder = new RoslynkForwarder(client);
	}

	public async Task<NuruForwardResult?> ForwardAsync(NuruApp app, NuruForwardRequest request, CancellationToken cancellationToken)
	{
		if (request.Mode == NuruForwardMode.Never)
			return null;

		NuruForwardResult? result = await Client.ForwardAsync(app, Required(request), cancellationToken).ConfigureAwait(false);
		if (result is null)
			return null; // This process is the server: it runs the call.

		if (result is { ExitCode: 1, Output.Length: 0, RenderedOutput.Length: 0 } && !await ServerAnswersAsync(app, cancellationToken).ConfigureAwait(false))
			return result with { ExitCode = ExitCodes.Unavailable };

		return result;
	}

	private async Task<bool> ServerAnswersAsync(NuruApp app, CancellationToken cancellationToken)
	{
		NuruDaemonStatus? status = await Client.PingAsync(app, cancellationToken).ConfigureAwait(false);
		return status is not null && status.BuildId == NuruDaemonPaths.GetBuildId(app);
	}

	private static NuruForwardRequest Required(NuruForwardRequest request)
	{
		if (request.Mode == NuruForwardMode.Require)
			return request;

		if (request.Args is { } args)
			return NuruForwardRequest.ForArgs(request.Route, args, request.JsonArguments, NuruForwardMode.Require);

		using JsonDocument arguments = JsonDocument.Parse(request.JsonArguments ?? "{}");
		return NuruForwardRequest.ForInvoke(request.Route, arguments.RootElement, NuruForwardMode.Require);
	}
}
