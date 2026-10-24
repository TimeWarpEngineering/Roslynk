var builder = DistributedApplication.CreateBuilder(args);

// The roslynk app's served daemon in the foreground ('roslynk serve'), so its logs and the engine's OTLP traces
// and metrics reach the dashboard. It listens on its own socket, so the daemon of an installed roslynk (another
// build, which would stop this one) is left alone: point a development client at it with the same
// NURU_DAEMON_SOCKET value (a relative name is a file in the per-user socket directory).
builder.AddProject<Projects.Morris_Roslynk_Cli>("roslynk")
	.WithArgs("serve")
	.WithEnvironment("NURU_DAEMON_SOCKET", "roslynk-apphost.sock");

builder.Build().Run();
