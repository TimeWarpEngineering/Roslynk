using Morris.Roslynk.Cli.Hosting;
using Morris.Roslynk.Cli.Output;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Every operation route is an [Operation] contract compiled in from the engine's Features folder; its handler
// forwards the call to the shared Roslynk daemon (see Forwarding/). The host routes run the daemon and the
// MCP stdio bridge in this process (see Hosting/).
NuruApp app = NuruApp.CreateBuilder()
	.WithName("roslynk")
	.UseTerminal(new VerbatimTerminal(new TimeWarpTerminal()))
	.DiscoverEndpoints()
	.Map("")
		.WithHandler(HostEntry.RunDaemon)
		.WithDescription("Run the shared Roslynk daemon (as 'daemon').")
		.AsCommand()
		.Done()
	.Map("daemon {*settings}")
		.WithHandler((string[] settings) => HostEntry.RunDaemon())
		.WithDescription("Run the shared Roslynk daemon (MCP over loopback HTTP). Settings are configuration arguments such as --Roslynk:Port=6502.")
		.AsCommand()
		.Done()
	.Map("stdio")
		.WithHandler(async () => await HostEntry.RunStdioBridgeAsync())
		.WithDescription("Serve MCP on stdin/stdout, bridged to the shared daemon (started when none listens).")
		.AsCommand()
		.Done()
	.Build();

return await app.RunAsync(args);
