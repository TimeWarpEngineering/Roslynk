using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Morris.Roslynk.Cli.Output;
using TimeWarp.Mediator;
using TimeWarp.Nuru;
using TimeWarp.Nuru.McpHost;

namespace Morris.Roslynk.Cli.Hosting;

/// <summary>
/// <c>roslynk stdio</c>: the MCP server an agent launches. It is Nuru's MCP host over this app's catalog, and its
/// tool calls go through the app's forwarder to the shared served daemon, so every MCP session and CLI call
/// uses one warm engine. The route is local-only (the MCP process itself is never forwarded) and is not
/// published as a tool.
/// </summary>
[LocalOnly]
[NuruRoute("stdio", Description = "Serve MCP on stdin/stdout. Tool calls run in the shared Roslynk server, started when none runs.")]
public sealed class StdioCommand : ICommand<Unit>
{
	public const string RouteName = "stdio";

	private const string ProbeOperation = "get_solution_status";

	public sealed class Handler(NuruApp app) : ICommandHandler<StdioCommand, Unit>
	{
		/// <summary>
		/// Makes sure a server answers, starting one when none does, then serves MCP until the client closes
		/// stdin. A server that cannot be started is one line on stderr and exit 69, before any MCP traffic.
		/// </summary>
		public async Task<Unit> Handle(StdioCommand command, CancellationToken cancellationToken)
		{
			if (await ProbeAsync(app, cancellationToken) is { } failure)
			{
				await app.Terminal.WriteErrorLineAsync(failure);
				NuruCallContext.CurrentExitCode = ExitCodes.Unavailable;
				return Unit.Value;
			}

			await ServeAsync(app, cancellationToken);
			return Unit.Value;
		}
	}

	/// <summary>
	/// Nuru's stdio MCP server (as <c>NuruMcpServer.RunStdioAsync</c> runs it) over the catalog without this
	/// route. Stdout carries the protocol, so stray console writes go to stderr while it runs.
	/// </summary>
	private static async Task ServeAsync(NuruApp app, CancellationToken cancellationToken)
	{
		McpServerOptions options = NuruMcpServer.CreateServerOptions(app, McpServerRegistration.CreateOptions());
		McpServerPrimitiveCollection<McpServerTool> tools = options.ToolCollection ?? [];
		foreach (McpServerTool self in tools.Where(tool => tool.ProtocolTool.Name == RouteName).ToList())
			tools.Remove(self);

#pragma warning disable RS0030 // The stdio host owns the process streams while it serves; see summary.
		(TextWriter Out, TextWriter Error) previous = (Console.Out, Console.Error);
		Console.SetOut(Console.Error);
		try
		{
			StdioServerTransport transport = new(options);
			await using (transport)
			{
				McpServer server = McpServer.Create(transport, options);
				await using (server)
				{
					try
					{
						await server.RunAsync(cancellationToken);
					}
					catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
					{
						// Stopping on request is a normal end of the session.
					}
				}
			}
		}
		finally
		{
			Console.SetOut(previous.Out);
			Console.SetError(previous.Error);
		}
#pragma warning restore RS0030
	}

	/// <summary>
	/// Runs the cheapest operation through the forwarder, which starts the server when none answers (and warms
	/// its engine). Returns the one-line failure when the server is unavailable, otherwise null.
	/// </summary>
	private static async Task<string?> ProbeAsync(NuruApp app, CancellationToken cancellationToken)
	{
		if (app.RouteForwarder is not INuruRouteForwarder forwarder)
			return null;

		EndpointCapability? probe = app.Capabilities.Endpoints
			.FirstOrDefault(endpoint => endpoint.Pattern.Split(' ')[0] == ProbeOperation);
		if (probe is null)
			return null;

		using JsonDocument noArguments = JsonDocument.Parse("{}");
		NuruForwardResult? result = await forwarder.ForwardAsync(
			app,
			NuruForwardRequest.ForInvoke(probe.Pattern, noArguments.RootElement, NuruForwardMode.Require),
			cancellationToken);
		return result is { ExitCode: ExitCodes.Unavailable } ? result.ErrorOutput.TrimEnd('\r', '\n') : null;
	}
}
