using Microsoft.Extensions.Configuration;
using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Outlines;

namespace Morris.Roslynk.Cli.Hosting;

/// <summary>
/// Every Roslynk engine setting, read from the 'Roslynk' section of the app's configuration (Nuru's sources:
/// appsettings.json beside the executable, environment variables, <c>--Roslynk:Key=value</c>). The engine
/// reads it once when it starts, so changing any value needs a server restart. Each property is nullable so
/// "not configured" is distinguishable from an explicit value; the Effective members apply the defaults.
/// </summary>
public sealed class RoslynkSettings
{
	public const string SectionName = "Roslynk";

	/// <summary>The idle window applied when 'Roslynk:IdleEvictionMinutes' is absent.</summary>
	public const double DefaultIdleEvictionMinutes = 30.0;

	/// <summary>The eviction sweep interval applied when 'Roslynk:EvictionCheckMinutes' is absent.</summary>
	public const double DefaultEvictionCheckMinutes = 5.0;

	/// <summary>The smallest eviction sweep interval; a smaller configured value is raised to it.</summary>
	public const double MinimumEvictionCheckMinutes = 1.0;

	/// <summary>The response budget in characters; absent means <see cref="ResponseBudget.DefaultMaxChars"/>.</summary>
	public int? MaxResponseChars { get; set; }

	/// <summary>Seconds a solution-scoped call waits for an initial load; absent means 30 seconds.</summary>
	public double? LoadWaitSeconds { get; set; }

	/// <summary>Minutes a solution may sit unused before eviction; absent means 30, zero or less disables eviction.</summary>
	public double? IdleEvictionMinutes { get; set; }

	/// <summary>Minutes between idle-eviction sweeps; absent means 5, floored at 1.</summary>
	public double? EvictionCheckMinutes { get; set; }

	/// <summary>Reads the 'Roslynk' section. Absent keys stay null so the Effective members own every default.</summary>
	public static RoslynkSettings FromConfiguration(IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		IConfigurationSection section = configuration.GetSection(SectionName);
		return new RoslynkSettings
		{
			MaxResponseChars = section.GetValue<int?>(nameof(MaxResponseChars)),
			LoadWaitSeconds = section.GetValue<double?>(nameof(LoadWaitSeconds)),
			IdleEvictionMinutes = section.GetValue<double?>(nameof(IdleEvictionMinutes)),
			EvictionCheckMinutes = section.GetValue<double?>(nameof(EvictionCheckMinutes)),
		};
	}

	/// <summary>
	/// The configured budget, or 80,000 when absent. A configured value below the minimum (including 0)
	/// is raised to <see cref="ResponseBudget.MinimumMaxChars"/>.
	/// </summary>
	public int EffectiveMaxChars =>
		MaxResponseChars is int value
			? Math.Max(ResponseBudget.MinimumMaxChars, value)
			: ResponseBudget.DefaultMaxChars;

	/// <summary>The configured load wait, or 30 seconds when absent; negative values clamp to zero.</summary>
	public TimeSpan EffectiveLoadWaitSeconds =>
		TimeSpan.FromSeconds(Math.Max(0, LoadWaitSeconds ?? LoadWaitTimeout.Default.Duration.TotalSeconds));

	/// <summary>The configured load wait as the engine's plain value.</summary>
	public LoadWaitTimeout EffectiveLoadWaitTimeout => new(EffectiveLoadWaitSeconds);

	/// <summary>The configured idle window, or 30 minutes when absent; zero or less disables eviction.</summary>
	public TimeSpan EffectiveIdleEvictionTimeout =>
		TimeSpan.FromMinutes(IdleEvictionMinutes ?? DefaultIdleEvictionMinutes);

	/// <summary>The configured eviction check interval, or 5 minutes when absent, floored at 1 minute.</summary>
	public TimeSpan EffectiveEvictionCheckInterval =>
		TimeSpan.FromMinutes(Math.Max(MinimumEvictionCheckMinutes, EvictionCheckMinutes ?? DefaultEvictionCheckMinutes));
}
