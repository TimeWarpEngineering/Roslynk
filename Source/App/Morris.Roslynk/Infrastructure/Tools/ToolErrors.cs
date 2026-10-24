using System.Text.Json;
using Morris.Roslynk.Infrastructure.Results;

namespace Morris.Roslynk.Infrastructure.Tools;

/// <summary>
/// The shared exception-to-<see cref="Error"/> rule for an exception that escaped a tool's own error
/// handling. The roslynk app's handlers apply it to a whole call, and multi_query to each operation, so a
/// slot's error block agrees exactly with what the standalone call returns.
/// </summary>
internal static class ToolErrors
{
	/// <summary>
	/// An argument the tool does not declare is UnknownArgument; any other binding failure (bad argument
	/// type, unparseable value) is the caller's fault and is Invalid; anything else is a fault in the tool
	/// itself and is Faulted. An <see cref="OperationCanceledException"/> is deliberately not mapped:
	/// callers rethrow it.
	/// </summary>
	public static Error ToError(Exception exception) =>
		exception is UnknownArgumentException
			? Error.UnknownArgument(exception.Message)
			: exception is ArgumentException or FormatException or JsonException or NotSupportedException
				? Error.Invalid(exception.Message)
				: Error.Faulted(exception.Message);
}
