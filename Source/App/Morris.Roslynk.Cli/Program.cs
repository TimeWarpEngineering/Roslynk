using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
	.WithName("roslynk-cli")
	.DiscoverEndpoints()
	.Build();

return await app.RunAsync(args);
