using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// The CLI's daemon settings, read like the stdio bridge reads them (appsettings.json beside the
/// executable, then environment variables, so <c>Roslynk__Port</c> wins). <c>Roslynk:Port</c> is the
/// daemon's own key; the <c>Roslynk:Cli</c> keys belong to this front end only.
/// </summary>
internal sealed class CliSettings
{
	/// <summary>The daemon's default port (<c>LoopbackOnlyExtensions.DefaultPort</c> in the host).</summary>
	public const int DefaultPort = 6502;

	public static string Version { get; } =
		typeof(CliSettings).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

	/// <summary>The loopback port the daemon listens on.</summary>
	public int Port { get; private init; } = DefaultPort;

	/// <summary>Start a daemon when none listens (default true).</summary>
	public bool AutoStart { get; private init; } = true;

	/// <summary>
	/// The daemon to start, overriding discovery: an executable, or a <c>.dll</c> run with <c>dotnet</c>.
	/// </summary>
	public string? Daemon { get; private init; }

	public static CliSettings Load()
	{
		IConfigurationRoot configuration = new ConfigurationBuilder()
			.SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", optional: true)
			.AddEnvironmentVariables()
			.Build();

		return new CliSettings
		{
			Port = configuration.GetValue("Roslynk:Port", DefaultPort),
			AutoStart = configuration.GetValue("Roslynk:Cli:AutoStart", true),
			Daemon = configuration.GetValue<string?>("Roslynk:Cli:Daemon"),
		};
	}
}
