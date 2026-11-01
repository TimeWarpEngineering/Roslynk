// A .NET startup hook must be a type named StartupHook in the global namespace.
#pragma warning disable CA1050

/// <summary>
/// Loaded into the CLI process through <c>DOTNET_STARTUP_HOOKS</c> by <c>ProcessIsolationTests</c>: when
/// <see cref="ListVariable"/> names a file, the simple names of every assembly loaded at exit are written
/// there. It touches nothing but the BCL, so it loads none of this test assembly's dependencies.
/// </summary>
internal static class StartupHook
{
	public const string ListVariable = "ROSLYNK_CLI_LOADED_ASSEMBLIES";

	public static void Initialize()
	{
		if (Environment.GetEnvironmentVariable(ListVariable) is not { Length: > 0 } path)
			return;

		AppDomain.CurrentDomain.ProcessExit += (_, _) =>
			File.WriteAllLines(path, AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetName().Name ?? ""));
	}
}
