using Morris.Roslynk.Mcp.Hosting;

// The development entry point of the host (the Aspire AppHost and `dotnet run` start it). The shipped
// `roslynk` command is the Nuru app in Morris.Roslynk.Cli, which runs the same two entry points.

// `stdio` runs the client-launched bridge instead of the daemon: it starts the daemon if needed
// and pipes one MCP session to it over HTTP.
if (args is ["stdio", ..])
{
	await StdioBridge.RunAsync();
	return;
}

// `daemon` is the unified command's explicit verb for the same host; the arguments after it configure it.
RoslynkDaemon.Run(args is ["daemon", .. string[] rest] ? rest : args);
