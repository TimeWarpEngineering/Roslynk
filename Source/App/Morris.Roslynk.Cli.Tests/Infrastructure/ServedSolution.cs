using System.Diagnostics;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Morris.Roslynk.Cli.Tests.Infrastructure;

/// <summary>
/// A served roslynk on a socket of its own, with a scratch copy of the Simple fixture open and loaded. The
/// first CLI call starts the server (self-start), so every test of a class shares one warm engine, as an
/// agent's CLI calls and MCP sessions do.
/// </summary>
public class ServedSolution : IAsyncLifetime
{
	public const string Calculator = "SimpleLibrary.Calculator";

	private string? ScratchRoot;

	public ServerSocket Socket { get; } = new();

	public string SolutionId { get; private set; } = "";

	public string SolutionDirectory { get; private set; } = "";

	public string SolutionPath { get; private set; } = "";

	public virtual async Task InitializeAsync()
	{
		ScratchRoot = Path.Combine(Path.GetTempPath(), $"roslynk-cli-tests-{Guid.NewGuid():N}");
		Fixtures.CopyDirectory(Path.Combine(Fixtures.Root, "SimpleSolution"), ScratchRoot);
		SolutionDirectory = ScratchRoot;
		SolutionPath = Path.Combine(ScratchRoot, "SimpleSolution.slnx");
		Fixtures.Restore(SolutionPath);

		CliResult opened = await RunAsync(["open_solution", "--solutionPath", SolutionPath]);
		if (opened.ExitCode != 0)
			throw new InvalidOperationException($"open_solution failed ({opened.ExitCode}): {opened.StandardError}");
		SolutionId = opened.StandardOutput.Split('\n').Single(line => line.StartsWith("solutionId=", StringComparison.Ordinal))["solutionId=".Length..];

		// Ready once a semantic query stops answering Indexing (exit 5).
		var clock = Stopwatch.StartNew();
		while ((await RunAsync(["get_symbol", "--solutionId", SolutionId, "--symbolName", Calculator])).ExitCode == 5)
		{
			if (clock.Elapsed > TimeSpan.FromMinutes(2))
				throw new TimeoutException("The Simple fixture did not finish loading.");
			await Task.Delay(250);
		}
	}

	internal Task<CliResult> RunAsync(IEnumerable<string> arguments, IReadOnlyDictionary<string, string?>? environment = null, string? workingDirectory = null) =>
		CliProcess.RunAsync(arguments, Socket, environment, workingDirectory);

	/// <summary>A new MCP session: <c>roslynk stdio</c> on this fixture's socket.</summary>
	public Task<McpClient> ConnectAsync() => Mcp.ConnectAsync(Socket);

	public virtual async Task DisposeAsync()
	{
		await Socket.DisposeAsync();
		if (ScratchRoot is not null && Directory.Exists(ScratchRoot))
			Directory.Delete(ScratchRoot, recursive: true);
	}
}

/// <summary>MCP helpers over <c>roslynk stdio</c>.</summary>
public static class Mcp
{
	public static Task<McpClient> ConnectAsync(ServerSocket socket) =>
		McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
		{
			Name = "roslynk",
			Command = CliProcess.DotnetHost,
			Arguments = [CliProcess.CliAssembly, "stdio"],
			EnvironmentVariables = CliProcess.Environment(socket),
		}));

	/// <summary>One <c>tools/call</c>: the text an agent reads and whether it is an error result.</summary>
	public static async Task<(string Text, bool IsError)> CallAsync(
		this McpClient client,
		string tool,
		Dictionary<string, object?> arguments,
		CancellationToken cancellationToken = default)
	{
		Dictionary<string, JsonElement> json = arguments.ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value));
		CallToolResult result = await client.CallToolAsync(new CallToolRequestParams { Name = tool, Arguments = json }, cancellationToken: cancellationToken);
		return (string.Concat(result.Content.OfType<TextContentBlock>().Select(block => block.Text)), result.IsError == true);
	}
}

/// <summary>The committed fixture solutions, copied and restored the way the engine tests do.</summary>
internal static class Fixtures
{
	public static string Root { get; } = FindTestFixturesRoot();

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

	public static void CopyDirectory(string source, string destination)
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

	public static void Restore(string solutionPath)
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
