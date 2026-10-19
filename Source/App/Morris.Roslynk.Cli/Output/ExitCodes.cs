namespace Morris.Roslynk.Cli.Output;

/// <summary>
/// The process exit code for each Roslynk error code. A problem carries its code in
/// <c>SharedProblemDetails.Status</c>, and Nuru exits with that status. 1 stays Nuru's own: a usage or
/// <c>--json-args</c> binding error, and any error code this table does not know.
/// </summary>
internal static class ExitCodes
{
	public const int Success = 0;
	public const int Usage = 1;
	public const int Invalid = 2;
	public const int NotFound = 3;
	public const int Ambiguous = 4;
	public const int Indexing = 5;
	public const int Stale = 6;
	public const int Conflict = 7;
	public const int NotSupported = 8;
	public const int Faulted = 9;
	public const int Truncated = 10;

	/// <summary>The daemon is not listening and could not be started, or the call got no answer (sysexits EX_UNAVAILABLE).</summary>
	public const int Unavailable = 69;

	/// <summary>The problem type of a transport failure; it never comes from the daemon.</summary>
	public const string UnavailableType = "Unavailable";

	public static int For(string? errorCode) => errorCode switch
	{
		"Invalid" => Invalid,
		"NotFound" => NotFound,
		"Ambiguous" => Ambiguous,
		"Indexing" => Indexing,
		"Stale" => Stale,
		"Conflict" => Conflict,
		"NotSupported" => NotSupported,
		"Faulted" => Faulted,
		"Truncated" => Truncated,
		UnavailableType => Unavailable,
		_ => Usage,
	};
}
