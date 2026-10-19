using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

internal sealed record CliResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Runs the built <c>roslynk</c> as its own process against a daemon port. Daemon start is off unless
/// a test turns it on, so no test can boot a real daemon by accident.
/// </summary>
internal static class CliProcess
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(90);
	private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

	public static string CliAssembly { get; } = typeof(CliProcess).Assembly
		.GetCustomAttributes<AssemblyMetadataAttribute>()
		.Single(attribute => attribute.Key == "RoslynkCliAssembly")
		.Value!;

	public static string DotnetHost =>
		Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

	public static async Task<CliResult> RunAsync(
		IEnumerable<string> arguments,
		int port,
		string? standardInput = null,
		IReadOnlyDictionary<string, string>? environment = null)
	{
		ProcessStartInfo startInfo = StartInfo(arguments, port, environment);

		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start roslynk.");
		Task<string> stdout = process.StandardOutput.ReadToEndAsync();
		Task<string> stderr = process.StandardError.ReadToEndAsync();
		if (standardInput is not null)
			await process.StandardInput.WriteAsync(standardInput);
		process.StandardInput.Close();

		using var timeout = new CancellationTokenSource(Timeout);
		try
		{
			await process.WaitForExitAsync(timeout.Token);
		}
		catch (OperationCanceledException)
		{
			process.Kill(entireProcessTree: true);
			throw new TimeoutException($"roslynk {string.Join(' ', startInfo.ArgumentList.Skip(1))} did not exit within {Timeout}.");
		}

		return new CliResult(process.ExitCode, await stdout, await stderr);
	}

	/// <summary>
	/// The start of <c>dotnet roslynk.dll</c> with every standard stream redirected, daemon start off unless
	/// <paramref name="environment"/> turns it on, and <c>Roslynk__Port</c> set to <paramref name="port"/>.
	/// </summary>
	public static ProcessStartInfo StartInfo(IEnumerable<string> arguments, int port, IReadOnlyDictionary<string, string>? environment = null)
	{
		var startInfo = new ProcessStartInfo(DotnetHost)
		{
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			StandardOutputEncoding = Utf8,
			StandardErrorEncoding = Utf8,
			StandardInputEncoding = Utf8,
		};
		startInfo.ArgumentList.Add(CliAssembly);
		foreach (string argument in arguments)
			startInfo.ArgumentList.Add(argument);

		startInfo.Environment["Roslynk__Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
		startInfo.Environment["Roslynk__Cli__AutoStart"] = "false";
		startInfo.Environment["NO_COLOR"] = "1";
		if (environment is not null)
			foreach ((string key, string value) in environment)
				startInfo.Environment[key] = value;

		return startInfo;
	}

	/// <summary>True once something accepts a connection on the loopback <paramref name="port"/>.</summary>
	public static async Task<bool> IsListeningAsync(int port)
	{
		using var probe = new TcpClient();
		try
		{
			await probe.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromMilliseconds(500));
			return true;
		}
		catch (Exception exception) when (exception is SocketException or TimeoutException)
		{
			return false;
		}
	}

	/// <summary>Waits up to <paramref name="timeout"/> for a listener on <paramref name="port"/>.</summary>
	public static async Task<bool> WaitForListenerAsync(int port, TimeSpan timeout)
	{
		var clock = Stopwatch.StartNew();
		while (clock.Elapsed < timeout)
		{
			if (await IsListeningAsync(port))
				return true;
			await Task.Delay(200);
		}

		return false;
	}

	/// <summary>A loopback port nothing listens on (bound once, then released).</summary>
	public static int UnusedPort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		int port = ((IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}
}
