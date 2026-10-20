extern alias engine;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using EngineServices = engine::Morris.Roslynk.ServicesRegistration;
using InstanceRegistry = engine::Morris.Roslynk.Infrastructure.Lifecycle.InstanceRegistry;
using ResponseBudget = engine::Morris.Roslynk.Infrastructure.Outlines.ResponseBudget;
using RoslynkActivitySource = engine::Morris.Roslynk.Infrastructure.Observability.RoslynkActivitySource;
using RoslynkMeter = engine::Morris.Roslynk.Infrastructure.Observability.RoslynkMeter;
using SolutionMetrics = engine::Morris.Roslynk.Infrastructure.Observability.SolutionMetrics;

namespace Morris.Roslynk.Cli.Hosting;

/// <summary>
/// The Roslynk engine of the served app: the engine's services (<c>AddRoslynk</c>), its tools, idle solution
/// eviction and optional OpenTelemetry export, composed on first use. A singleton of the app, so every call
/// the server runs shares one warm <see cref="InstanceRegistry"/>. Public because Nuru's generated DI
/// constructs it.
/// </summary>
/// <remarks>
/// Nothing here runs until a handler asks for the engine. Nuru constructs singletons on every CLI call, and a
/// forwarded call (the usual one) must not load Roslyn in the client, so the constructor only keeps the
/// configuration. Settings, from any Nuru configuration source and read once when the engine starts:
/// <c>Roslynk:MaxResponseChars</c> (the server-wide response budget), <c>Roslynk:IdleEvictionMinutes</c>
/// (default 30; zero or less keeps solutions loaded), <c>Roslynk:EvictionCheckMinutes</c> (default 5), and
/// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> (when set, the engine's traces and metrics are exported over OTLP).
/// </remarks>
public sealed class RoslynkEngine : IDisposable
{
	public const string MaxResponseCharsKey = "Roslynk:MaxResponseChars";
	public const string IdleEvictionMinutesKey = "Roslynk:IdleEvictionMinutes";
	public const string EvictionCheckMinutesKey = "Roslynk:EvictionCheckMinutes";
	public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

	private readonly Lazy<Composition> Composed;

	public RoslynkEngine(IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		Composed = new Lazy<Composition>(() => Composition.Start(configuration), LazyThreadSafetyMode.ExecutionAndPublication);
	}

	/// <summary>The engine's services: the registry, the tools and the response budget.</summary>
	public IServiceProvider Services => Composed.Value.Provider;

	public InstanceRegistry Registry => Services.GetRequiredService<InstanceRegistry>();

	public ResponseBudget Budget => Services.GetRequiredService<ResponseBudget>();

	/// <summary>The engine tool that owns an operation's handler.</summary>
	public TTool Tool<TTool>() where TTool : class => Services.GetRequiredService<TTool>();

	public void Dispose()
	{
		if (Composed.IsValueCreated)
			Composed.Value.Dispose();
	}

	/// <summary>The composed engine. A separate type, so only its use loads the engine and Roslyn.</summary>
	private sealed class Composition : IDisposable
	{
		private readonly CancellationTokenSource Stopping = new();
		private readonly List<IDisposable> Telemetry = [];

		private Composition(ServiceProvider provider)
		{
			Provider = provider;
		}

		public ServiceProvider Provider { get; }

		public static Composition Start(IConfiguration configuration)
		{
			ServiceCollection services = new();
			services.AddRoslynkEngine(new ResponseBudget(configuration.GetValue(MaxResponseCharsKey, ResponseBudget.DefaultMaxChars)));

			Composition composition = new(services.BuildServiceProvider());
			if (!string.IsNullOrWhiteSpace(configuration[OtlpEndpointKey]))
				composition.ExportTelemetry();

			TimeSpan idleTimeout = TimeSpan.FromMinutes(configuration.GetValue(IdleEvictionMinutesKey, 30.0));
			TimeSpan checkInterval = TimeSpan.FromMinutes(Math.Max(1.0, configuration.GetValue(EvictionCheckMinutesKey, 5.0)));
			if (idleTimeout > TimeSpan.Zero)
				_ = composition.EvictIdleAsync(idleTimeout, checkInterval);
			return composition;
		}

		public void Dispose()
		{
			Stopping.Cancel();
			foreach (IDisposable provider in Telemetry)
				provider.Dispose();
			Provider.Dispose();
			Stopping.Dispose();
		}

		/// <summary>
		/// The engine's traces (each operation and the instance lifecycle) and metrics (open solutions, load
		/// durations) over the OTLP exporter, which reads its endpoint from the standard <c>OTEL_EXPORTER_OTLP_*</c>
		/// settings (the Aspire AppHost sets them).
		/// </summary>
		private void ExportTelemetry()
		{
			Telemetry.Add(Sdk.CreateTracerProviderBuilder()
				.ConfigureResource(resource => resource.AddService(serviceName: "Roslynk"))
				.AddSource(RoslynkActivitySource.Name)
				.AddOtlpExporter()
				.Build());
			Telemetry.Add(Sdk.CreateMeterProviderBuilder()
				.ConfigureResource(resource => resource.AddService(serviceName: "Roslynk"))
				.AddMeter(RoslynkMeter.Name)
				.AddOtlpExporter()
				.Build());

			// The open-solutions gauge registers when its publisher is constructed.
			Provider.GetRequiredService<SolutionMetrics>();
		}

		/// <summary>
		/// Closes solutions nobody used for <paramref name="idleTimeout"/>, beside the server's own idle stop:
		/// a server kept busy by one solution still releases the others. The decision lives in the registry.
		/// </summary>
		private async Task EvictIdleAsync(TimeSpan idleTimeout, TimeSpan checkInterval)
		{
			using PeriodicTimer timer = new(checkInterval);
			try
			{
				InstanceRegistry registry = Provider.GetRequiredService<InstanceRegistry>();
				while (await timer.WaitForNextTickAsync(Stopping.Token))
					registry.EvictIdle(idleTimeout, DateTime.UtcNow);
			}
			catch (OperationCanceledException)
			{
				// The engine stopped.
			}
		}
	}
}

/// <summary>The engine composition the served app runs; the tests compose the same way.</summary>
internal static class RoslynkEngineRegistration
{
	/// <summary><c>AddRoslynk</c> plus one singleton per engine tool, the type that owns each operation's handler.</summary>
	public static IServiceCollection AddRoslynkEngine(this IServiceCollection services, ResponseBudget? budget = null)
	{
		EngineServices.AddRoslynk(services, budget);
		foreach (Type tool in typeof(EngineServices).Assembly.GetTypes().Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null))
			services.AddSingleton(tool);
		return services;
	}
}
