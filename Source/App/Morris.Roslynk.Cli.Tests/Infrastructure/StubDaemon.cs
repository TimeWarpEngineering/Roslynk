using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

/// <summary>
/// A loopback MCP server over streamable HTTP that records every <c>tools/call</c> and answers with
/// whatever <see cref="Respond"/> returns. It stands in for the daemon wherever the test is about what the
/// CLI sends or how it prints, not about what the engine computes. One per test class (a class fixture).
/// </summary>
public sealed class StubDaemon : IAsyncLifetime
{
	private WebApplication? App;

	public int Port { get; private set; }

	public ConcurrentQueue<CallToolRequestParams> Calls { get; } = new();

	public Func<CallToolRequestParams, CallToolResult> Respond { get; set; } = _ => Text("ok");

	public static CallToolResult Text(string text, bool isError = false) =>
		new() { Content = [new TextContentBlock { Text = text }], IsError = isError };

	public async Task InitializeAsync()
	{
		WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services
			.AddMcpServer()
			.WithHttpTransport()
			.WithListToolsHandler((_, _) => ValueTask.FromResult(new ListToolsResult { Tools = [] }))
			.WithCallToolHandler((context, _) =>
			{
				CallToolRequestParams call = context.Params ?? throw new InvalidOperationException("tools/call without parameters.");
				Calls.Enqueue(call);
				return ValueTask.FromResult(Respond(call));
			});

		App = builder.Build();
		App.MapMcp();
		await App.StartAsync();

		string address = App.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
		Port = new Uri(address).Port;
	}

	/// <summary>Clears the recorded calls and the canned answer before a test sets its own.</summary>
	public void Reset()
	{
		Calls.Clear();
		Respond = _ => Text("ok");
	}

	public async Task DisposeAsync()
	{
		if (App is not null)
			await App.DisposeAsync();
	}
}
