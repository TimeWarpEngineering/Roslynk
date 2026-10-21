using Microsoft.Extensions.DependencyInjection;
using Morris.Roslynk.Cli.Hosting;
using Morris.Roslynk.Features.Symbols.GetSymbol;
using TimeWarp.Nuru;

// The engine's [Operation] request classes are this app's routes: the attribute adds every one in the engine
// assembly (named by any type in it) to DiscoverEndpoints().
[assembly: NuruEndpointAssembly(typeof(GetSymbol.Query))]

// One Nuru app is the whole of Roslynk. Every operation route is an [Operation] contract declared in the
// engine, handled by Handlers/ on the engine. 'serve' runs those handlers in the shared daemon (Nuru served
// app); every operation call forwards to it, starting it when none runs. 'stdio' is the MCP server (Nuru
// McpHost, Hosting/StdioCommand.cs) whose tool calls forward the same way. A binding error or an escaped
// exception renders through the app's problem renderer (error=Invalid / error=Faulted) on every path.
NuruApp app = NuruApp.CreateBuilder()
	.WithName("roslynk")
	.UseVerbatimRendererOutput()
	.UseProblemRendererForErrors()
	.ConfigureServices(services => services.AddSingleton<RoslynkEngine>())
	.DiscoverEndpoints()
	.AddServeRoute(idleTimeoutSeconds: 3600, autoStart: true)
	.Build();

RoslynkForwarder.Attach(app);
return await app.RunAsync(args);
