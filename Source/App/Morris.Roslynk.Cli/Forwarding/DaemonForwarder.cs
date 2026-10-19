using System.Text;
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
				new McpClientOptions { ClientInfo = new Implementation { Name = "roslynk-cli", Version = CliSettings.Version } },
				cancellationToken: cancellationToken);

			CallToolResult result = await daemon.CallToolAsync(
				new CallToolRequestParams { Name = call.ToolName, Arguments = call.Arguments },
				cancellationToken);

			return new ForwardedText(TextOf(result), result.IsError == true, IsTransportFailure: false);
		}
		catch (Exception exception) when (exception is HttpRequestException or McpException or IOException or TimeoutException)
		{
			return new ForwardedText(
				$"roslynk-cli: the Roslynk daemon on port {settings.Port} did not answer {call.ToolName}: {OneLine(exception.Message)}",
				IsError: true,
				IsTransportFailure: true);
		}
	}

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
