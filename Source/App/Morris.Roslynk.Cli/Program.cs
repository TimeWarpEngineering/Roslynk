using Morris.Roslynk.Cli.Output;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

// Every route is an [Operation] contract compiled in from the engine's Features folder; its handler
// forwards the call to the shared Roslynk daemon (see Forwarding/).
NuruApp app = NuruApp.CreateBuilder()
	.WithName("roslynk-cli")
	.UseTerminal(new VerbatimTerminal(new TimeWarpTerminal()))
	.DiscoverEndpoints()
	.Build();

return await app.RunAsync(args);
