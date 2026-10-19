using Morris.Roslynk.Infrastructure.Observability;
using Morris.Roslynk.Mcp.Observability;

namespace Morris.Roslynk.Mcp.Hosting;

/// <summary>
/// The shared HTTP daemon: the engine, the MCP tools over streamable HTTP on the loopback port, idle
/// eviction and observability. The <c>roslynk</c> command runs it with no arguments (or <c>daemon</c>), and
/// this project's own entry point runs it for the Aspire host and development.
/// </summary>
public static class RoslynkDaemon
{
	/// <summary>Runs the daemon until it is stopped. <paramref name="args"/> are configuration arguments.</summary>
	public static void Run(string[] args)
	{
		WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

		// Safe on every OS: AddWindowsService only takes effect when the process is launched by the
		// Windows Service Control Manager; otherwise it registers nothing.
		builder.Services.AddWindowsService(options => options.ServiceName = "Roslynk");

		builder.AddLoopbackOnlyKestrel();
		builder.AddRoslynkObservability();

		builder.Services.AddRoslynk(ResponseBudgetConfiguration.FromConfiguration(builder.Configuration));
		builder.Services.AddHostedService<IdleEvictionService>();

		builder.Services
			.AddMcpServer(McpServerRegistration.Configure)
			.WithHttpTransport()
			.WithRoslynkTools();

		WebApplication app = builder.Build();

		// Eagerly create the metrics publisher so its observable instrument is registered for the process
		// lifetime; without a resolve the singleton would never be constructed and the metric never appear.
		app.Services.GetRequiredService<SolutionMetrics>();

		app.MapMcp();

		app.Run();
	}
}
