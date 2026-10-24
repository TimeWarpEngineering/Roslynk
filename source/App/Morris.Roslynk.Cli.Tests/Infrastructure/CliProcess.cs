using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

internal sealed record CliResult(int ExitCode, string StandardOutput, string StandardError);

/// <summary>
/// Runs the built <c>roslynk</c> as its own process, on a test's own server socket (<see cref="ServerSocket"/>),
/// so no test reaches the user's server or another test's.
/// </summary>
internal static class CliProcess
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);
	private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

	public static string CliAssembly { get; } = typeof(CliProcess).Assembly
		.GetCustomAttributes<AssemblyMetadataAttribute>()
		.Single(attribute => attribute.Key == "RoslynkCliAssembly")
		.Value!;

	public static string DotnetHost =>
		System.Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } host ? host : "dotnet";

	public static async Task<CliResult> RunAsync(
		IEnumerable<string> arguments,
		ServerSocket socket,
		IReadOnlyDictionary<string, string?>? environment = null,
		string? workingDirectory = null)
	{
		ProcessStartInfo startInfo = StartInfo(arguments, socket, environment);
		if (workingDirectory is not null)
			startInfo.WorkingDirectory = workingDirectory;

		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start roslynk.");
		Task<string> stdout = process.StandardOutput.ReadToEndAsync();
		Task<string> stderr = process.StandardError.ReadToEndAsync();
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
	/// The start of <c>dotnet roslynk.dll</c> with every standard stream redirected, on <paramref name="socket"/>,
	/// and with the environment a server it starts should have (see <see cref="Environment"/>).
	/// </summary>
	public static ProcessStartInfo StartInfo(IEnumerable<string> arguments, ServerSocket socket, IReadOnlyDictionary<string, string?>? environment = null)
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

		foreach ((string key, string? value) in Environment(socket, environment))
		{
			if (value is null)
				startInfo.Environment.Remove(key);
			else
				startInfo.Environment[key] = value;
		}

		return startInfo;
	}

	/// <summary>
	/// The variables a roslynk process gets: the test's socket, no color, and none of the MSBuild variables the
	/// test host sets, which would point a server it starts at the wrong MSBuild (a null value removes one).
	/// </summary>
	public static Dictionary<string, string?> Environment(ServerSocket socket, IReadOnlyDictionary<string, string?>? extra = null)
	{
		Dictionary<string, string?> variables = new(StringComparer.Ordinal)
		{
			["NURU_DAEMON_SOCKET"] = socket.Path,
			["NO_COLOR"] = "1",
			["MSBuildSDKsPath"] = null,
			["MSBuildExtensionsPath"] = null,
			["MSBUILD_EXE_PATH"] = null,
			["MSBUILDDISABLENODEREUSE"] = "1",
			["MSBUILDUSESERVER"] = "0",
			["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1",
		};
		if (extra is not null)
			foreach ((string key, string? value) in extra)
				variables[key] = value;
		return variables;
	}
}
