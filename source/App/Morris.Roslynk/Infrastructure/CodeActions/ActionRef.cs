namespace Morris.Roslynk.Infrastructure.CodeActions;

/// <summary>
/// The stateless identity of a discovered action, encoded into the <c>actionId</c> a client passes back:
/// the document, the span it was found at, its kind, and its equivalence key (or title). Apply re-runs
/// discovery at that document and span and matches on kind + key, so nothing is held between calls.
/// For a Razor source, <see cref="GeneratedDocument"/> names which of its generated documents the span is in.
/// </summary>
public sealed class ActionRef
{
	public string DocumentPath { get; }
	public int SpanStart { get; }
	public int SpanLength { get; }
	public string Kind { get; }
	public string Key { get; }

	/// <summary>The file name of the Razor-generated document the span is in, or null (a .cs document, or an older id).</summary>
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public string? GeneratedDocument { get; }

	public ActionRef(string documentPath, int spanStart, int spanLength, string kind, string key, string? generatedDocument = null)
	{
		GeneratedDocument = generatedDocument;
		DocumentPath = documentPath;
		SpanStart = spanStart;
		SpanLength = spanLength;
		Kind = kind;
		Key = key;
	}
}
