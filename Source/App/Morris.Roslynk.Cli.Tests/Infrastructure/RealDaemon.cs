using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Morris.Roslynk;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

/// <summary>
/// The daemon's own composition (<c>AddRoslynk</c> + <c>WithRoslynkTools</c> over streamable HTTP) on a
/// loopback port, with a scratch copy of the Simple fixture open. Tests compare what an MCP client reads
/// with what the CLI prints for the same call.
/// </summary>
public sealed class RealDaemon : IAsyncLifetime
{
	private WebApplication? App;
	private string? ScratchRoot;

	public int Port { get; private set; }

	public McpClient Client { get; private set; } = null!;

	public string SolutionId { get; private set; } = "";

	public string SolutionDirectory { get; private set; } = "";

	public async Task InitializeAsync()
	{
		ScratchRoot = Path.Combine(Path.GetTempPath(), $"roslynk-tests-{Guid.NewGuid():N}");
		string source = Path.Combine(FindTestFixturesRoot(), "SimpleSolution");
		CopyDirectory(source, ScratchRoot);
		SolutionDirectory = ScratchRoot;
		string solutionPath = Path.Combine(ScratchRoot, "SimpleSolution.slnx");
		Restore(solutionPath);

		WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddRoslynk();
		builder.Services.AddMcpServer().WithHttpTransport().WithRoslynkTools();

		App = builder.Build();
		App.MapMcp();
		await App.StartAsync();
		Port = new Uri(App.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()).Port;

		Client = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
		{
			Endpoint = new Uri($"http://localhost:{Port}"),
			TransportMode = HttpTransportMode.StreamableHttp,
		}));

		string opened = await CallAsync("open_solution", new() { ["solutionPath"] = solutionPath });
		SolutionId = opened.Split('\n').Single(line => line.StartsWith("solutionId=", StringComparison.Ordinal))["solutionId=".Length..];

		// Ready once a semantic query stops answering Indexing.
		var stopwatch = Stopwatch.StartNew();
		while ((await CallAsync("get_symbol", new() { ["solutionId"] = SolutionId, ["symbolName"] = "SimpleLibrary.Calculator" }))
			.StartsWith("error=Indexing", StringComparison.Ordinal))
		{
			if (stopwatch.Elapsed > TimeSpan.FromMinutes(2))
				throw new TimeoutException("The Simple fixture did not finish loading.");
			await Task.Delay(250);
		}
	}

	/// <summary>The text an MCP client reads for one call.</summary>
	public async Task<string> CallAsync(string tool, Dictionary<string, object?> arguments)
	{
		Dictionary<string, JsonElement> json = arguments.ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value));
		CallToolResult result = await Client.CallToolAsync(new CallToolRequestParams { Name = tool, Arguments = json });
		return string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text));
	}

	public async Task DisposeAsync()
	{
		if (Client is not null)
			await Client.DisposeAsync();
		if (App is not null)
			await App.DisposeAsync();
		if (ScratchRoot is not null && Directory.Exists(ScratchRoot))
			Directory.Delete(ScratchRoot, recursive: true);
	}

	private static string FindTestFixturesRoot()
	{
		for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			string candidate = Path.Combine(directory.FullName, "TestFixtures");
			if (Directory.Exists(candidate))
				return candidate;
		}

		throw new DirectoryNotFoundException("Could not locate the TestFixtures folder above the test assembly.");
	}

	private static void CopyDirectory(string source, string destination)
	{
		Directory.CreateDirectory(destination);
		foreach (string file in Directory.EnumerateFiles(source))
			File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
		foreach (string directory in Directory.EnumerateDirectories(source))
		{
			string name = Path.GetFileName(directory);
			if (name is "bin" or "obj")
				continue;
			CopyDirectory(directory, Path.Combine(destination, name));
		}
	}

	private static void Restore(string solutionPath)
	{
		// Same isolation as the engine tests' TestSolutions: no build servers, no MSBuild variables
		// inherited from the test host.
		var startInfo = new ProcessStartInfo(CliProcess.DotnetHost)
		{
			ArgumentList = { "restore", solutionPath, "--disable-build-servers" },
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
		};
		startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
		startInfo.Environment["MSBUILDUSESERVER"] = "0";
		startInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
		startInfo.Environment.Remove("MSBuildSDKsPath");
		startInfo.Environment.Remove("MSBuildExtensionsPath");
		startInfo.Environment.Remove("MSBUILD_EXE_PATH");

		using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start dotnet restore.");
		Task<string> stdout = process.StandardOutput.ReadToEndAsync();
		Task<string> stderr = process.StandardError.ReadToEndAsync();
		process.WaitForExit();
		if (process.ExitCode != 0)
			throw new InvalidOperationException($"dotnet restore failed:\n{stdout.Result}\n{stderr.Result}");
	}
}
