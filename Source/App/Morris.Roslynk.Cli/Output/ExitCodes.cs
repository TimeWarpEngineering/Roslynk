namespace Morris.Roslynk.Cli.Output;

/// <summary>
/// The process exit code for each Roslynk error code. A handler stores it in
/// <c>SharedProblemDetails.Status</c>, and Nuru exits with that status. 1 stays Nuru's own: a usage or
/// <c>--json-args</c> binding error.
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

	/// <summary>The server is not running and could not be started, or the call got no answer (sysexits EX_UNAVAILABLE).</summary>
	public const int Unavailable = 69;

	/// <summary>An <c>error=</c> code this table does not know (sysexits EX_SOFTWARE), kept apart from a usage error.</summary>
	public const int UnknownError = 70;

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
		_ => UnknownError,
	};
}
