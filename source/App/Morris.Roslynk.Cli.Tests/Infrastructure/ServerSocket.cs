namespace Morris.Roslynk.Cli.Tests.Infrastructure;

/// <summary>
/// A server socket of a test's own: an owner-only temporary directory holding <c>roslynk.sock</c>, passed to
/// every roslynk process as <c>NURU_DAEMON_SOCKET</c>. Disposing stops the server that listens there (if any)
/// and deletes the directory.
/// </summary>
public sealed class ServerSocket : IAsyncDisposable
{
	public ServerSocket()
	{
		// Short: a Unix socket path is limited to about 104 bytes, and macOS's temp path is long.
		string root = OperatingSystem.IsWindows() ? System.IO.Path.GetTempPath() : "/tmp";
		Directory = System.IO.Path.Combine(root, $"rk-{Guid.NewGuid():N}"[..15]);
		if (OperatingSystem.IsWindows())
			System.IO.Directory.CreateDirectory(Directory);
		else
			System.IO.Directory.CreateDirectory(Directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

		Path = System.IO.Path.Combine(Directory, "roslynk.sock");
	}

	public string Directory { get; }

	public string Path { get; }

	/// <summary>True while a server answers on this socket (<c>roslynk serve --status</c>).</summary>
	public async Task<bool> IsServingAsync() =>
		(await CliProcess.RunAsync(["serve", "--status"], this)).ExitCode == 0;

	public async ValueTask DisposeAsync()
	{
		try
		{
			await CliProcess.RunAsync(["serve", "--stop"], this);
		}
		finally
		{
			if (System.IO.Directory.Exists(Directory))
				System.IO.Directory.Delete(Directory, recursive: true);
		}
	}
}
