using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Morris.Roslynk.Infrastructure.CodeActions;
using Morris.Roslynk.Infrastructure.Diagnostics;
using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Observability;
using Morris.Roslynk.Infrastructure.Outlines;
using Morris.Roslynk.Infrastructure.Projections;
using Morris.Roslynk.Infrastructure.Resolution;
using Morris.Roslynk.Infrastructure.Writing;
using TimeWarp.Contracts;

namespace Morris.Roslynk;

public static class ServicesRegistration
{
	/// <summary>
	/// Registers Roslynk's engine and feature services. Registrations are added here as each
	/// Infrastructure area and feature slice is built. The optional <paramref name="responseBudget"/> caps
	/// every tool result; hosts that read it from configuration pass it here (see the Roslynk MCP host's
	/// ResponseBudgetConfiguration), otherwise the default applies.
	/// </summary>
	public static IServiceCollection AddRoslynk(this IServiceCollection services, ResponseBudget? responseBudget = null)
	{
		services.AddSingleton(responseBudget ?? ResponseBudget.Default);
		services.AddSingleton<InstanceRegistry>();
		services.AddSingleton<DiagnosticsService>();
		services.AddSingleton<SymbolResolver>();
		services.AddSingleton<ProjectionService>();
		services.AddSingleton<ConditionalCoverage>();
		services.AddSingleton<ApplyPipeline>();
		services.AddSingleton<DocumentDiagnosticsProvider>();
		services.AddSingleton<CodeActionService>();
		services.AddSingleton(provider => new SolutionMetrics(RoslynkMeter.Instance, provider.GetRequiredService<InstanceRegistry>()));
		RegisterResponseRenderers(services);
		return services;
	}

	/// <summary>
	/// One singleton per <see cref="IResponseRenderer{TResponse}"/>. The MCP methods also keep a nested
	/// renderer instance, so a provider built without this scan can still render.
	/// </summary>
	private static void RegisterResponseRenderers(IServiceCollection services)
	{
		foreach (Type type in typeof(ServicesRegistration).Assembly.GetTypes())
		{
			if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters)
				continue;

			foreach (Type service in type.GetInterfaces())
			{
				if (service.IsGenericType && service.GetGenericTypeDefinition() == typeof(IResponseRenderer<>))
				{
					// The renderer types are internal. A factory compiled into this assembly can construct
					// them; the container's own activator cannot.
					services.AddSingleton(service, _ => Activator.CreateInstance(type)!);
				}
			}
		}
	}
}
