using System.Text;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// What the daemon answered: the text content of the <c>tools/call</c> result and its <c>isError</c> flag,
/// or, when the call never got an answer, the one line that says why and how to start the daemon.
/// </summary>
internal sealed record ForwardedText(string Text, bool IsError, bool IsTransportFailure);

/// <summary>
/// Sends one <c>tools/call</c> to the shared Roslynk daemon, the way the stdio bridge does: resolve the
/// port, make sure a daemon listens there (starting one when needed), open one MCP client over streamable
/// HTTP and dispose it afterwards, so the daemon releases this session's solution ref-counts.
/// </summary>
internal static class DaemonForwarder
{
	public static async Task<ForwardedText> CallAsync(ForwardedCall call, CancellationToken cancellationToken)
	{
		CliSettings settings = CliSettings.Load();

		string? unavailable = await DaemonStarter.EnsureRunningAsync(settings, cancellationToken);
		if (unavailable is not null)
			return new ForwardedText(unavailable, IsError: true, IsTransportFailure: true);

		// One window for the whole call, connect to answer, so a daemon that accepts and then stalls cannot
		// hang the CLI. The caller's token (Ctrl+C) still cancels as cancellation.
		using var window = new CancellationTokenSource(settings.Timeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(window.Token, cancellationToken);
		try
		{
			var transport = new HttpClientTransport(new HttpClientTransportOptions
			{
				Endpoint = new Uri($"http://localhost:{settings.Port}"),
				TransportMode = HttpTransportMode.StreamableHttp,
				Name = "Roslynk daemon",
			});

			await using McpClient daemon = await McpClient.CreateAsync(
				transport,
				new McpClientOptions { ClientInfo = new Implementation { Name = "roslynk", Version = CliSettings.Version } },
				cancellationToken: linked.Token);

			CallToolResult result = await daemon.CallToolAsync(
				new CallToolRequestParams { Name = call.ToolName, Arguments = call.Arguments },
				linked.Token);

			return new ForwardedText(TextOf(result), result.IsError == true, IsTransportFailure: false);
		}
		catch (OperationCanceledException) when (window.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
		{
			return Unavailable(settings, call, $"no answer within {settings.Timeout.TotalSeconds:0}s (Roslynk:Cli:TimeoutSeconds)");
		}
		catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
		{
			// HttpClient reports its own timeout as a TaskCanceledException that no caller asked for.
			return Unavailable(settings, call, exception.Message);
		}
		catch (Exception exception) when (exception is HttpRequestException or McpException or IOException or TimeoutException or JsonException)
		{
			return Unavailable(settings, call, exception.Message);
		}
	}

	private static ForwardedText Unavailable(CliSettings settings, ForwardedCall call, string reason) =>
		new(
			$"roslynk: the Roslynk daemon on port {settings.Port} did not answer {call.ToolName}: {OneLine(reason)}",
			IsError: true,
			IsTransportFailure: true);

	/// <summary>Every Roslynk tool answers with one text block; several are joined unchanged.</summary>
	private static string TextOf(CallToolResult result)
	{
		if (result.Content is [TextContentBlock single])
			return single.Text;

		var text = new StringBuilder();
		foreach (ContentBlock block in result.Content)
			if (block is TextContentBlock textBlock)
				text.Append(textBlock.Text);
		return text.ToString();
	}

	internal static string OneLine(string message) => message.ReplaceLineEndings(" ").Trim();
}
