using Microsoft.Extensions.DependencyInjection;
using Morris.Roslynk.Cli.Hosting;
using TimeWarp.Nuru;

// One Nuru app is the whole of Roslynk. Every operation route is an [Operation] contract compiled in from the
// engine's Features folder, handled by Handlers/ on the engine. 'serve' runs those handlers in the shared
// daemon (Nuru served app); every operation call forwards to it, starting it when none runs. 'stdio' is the
// MCP server (Nuru McpHost, Hosting/StdioCommand.cs) whose tool calls forward the same way.
NuruApp app = NuruApp.CreateBuilder()
	.WithName("roslynk")
	.UseVerbatimRendererOutput()
	.ConfigureServices(services => services.AddSingleton<RoslynkEngine>())
	.DiscoverEndpoints()
	.AddServeRoute(idleTimeoutSeconds: 3600, autoStart: true)
	.Build();

RoslynkForwarder.Attach(app);
return await app.RunAsync(args);
