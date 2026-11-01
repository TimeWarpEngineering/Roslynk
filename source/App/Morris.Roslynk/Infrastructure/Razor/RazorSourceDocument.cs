using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Morris.Roslynk.Infrastructure.Results;
using Morris.Roslynk.Infrastructure.Workspaces;

namespace Morris.Roslynk.Infrastructure.Razor;

/// <summary>
/// The C# document a tool operates on for a user-supplied path. For a <c>.cs</c> file it is that document.
/// For a <c>.razor</c>/<c>.cshtml</c> file it is the Razor-generated C# document, together with the Razor
/// source text, so line/column positions in the Razor file can be mapped to the generated code Roslyn
/// analyses. Edits Roslyn makes to the generated document are mapped back with
/// <see cref="RazorGeneratedChangeFolder"/>.
/// <para>
/// One Razor file can have several generated documents: from the .NET 11 Razor compiler on, a component's
/// declarations (the <c>@code</c> members) are emitted to <c>X_razor.decl.g.cs</c> beside the render tree in
/// <c>X_razor.g.cs</c>. A position is mapped through whichever of them holds the C# copied from it, so callers
/// take the document from <see cref="MapToDocumentAsync"/> rather than assuming <see cref="Document"/>.
/// </para>
/// </summary>
public sealed class RazorSourceDocument
{
	/// <summary>The document Roslyn operates on: the .cs file itself, or the Razor file's primary generated C#.</summary>
	public Document Document { get; }

	/// <summary>
	/// Every document the source's C# lives in: the .cs file itself, or each Razor-generated document of the file,
	/// <see cref="Document"/> first.
	/// </summary>
	public IReadOnlyList<Document> Documents { get; }

	/// <summary>The Razor source path, or null for an ordinary .cs document.</summary>
	public string? RazorPath { get; }

	/// <summary>The Razor source text, or null for an ordinary .cs document.</summary>
	public SourceText? RazorText { get; }

	/// <summary>The canonical solution-relative path of the file the caller named (the .cs, .razor or .cshtml).</summary>
	public string ResolvedPath { get; }

	/// <summary>True when the caller's path was not an exact match and resolved through the unique-suffix fallback.</summary>
	public bool ResolvedBySuffix { get; }

	public bool IsRazor => RazorPath is not null;

	private RazorSourceDocument(IReadOnlyList<Document> documents, string? razorPath, SourceText? razorText, string resolvedPath, bool resolvedBySuffix)
	{
		Document = documents[0];
		Documents = documents;
		RazorPath = razorPath;
		RazorText = razorText;
		ResolvedPath = resolvedPath;
		ResolvedBySuffix = resolvedBySuffix;
	}

	public static bool IsRazorSourcePath(string path) =>
		path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
		|| path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Resolves <paramref name="path"/> (absolute or solution-relative, or a unique suffix/file name) to a .cs
	/// document, or to the generated document of a .razor/.cshtml file. Returns null when the path names no
	/// loaded document, or when the fallback matches several files and none can be chosen.
	/// </summary>
	public static async Task<RazorSourceDocument?> ResolveAsync(Solution solution, string path, CancellationToken cancellationToken = default) =>
		(await ResolveWithErrorAsync(solution, path, null, cancellationToken)).Source;

	/// <summary>
	/// Resolves <paramref name="path"/> like <see cref="ResolveAsync"/>, but carries the shared path
	/// classification so a tool can report several matching files as <c>Ambiguous</c> instead of a generic
	/// <c>NotFound</c>. When <paramref name="notFoundMessage"/> is null, a genuine miss carries no message
	/// (callers using this overload only need to distinguish ambiguity).
	/// </summary>
	public static async Task<(RazorSourceDocument? Source, Error? Error)> ResolveWithErrorAsync(
		Solution solution,
		string path,
		string? notFoundMessage,
		CancellationToken cancellationToken = default)
	{
		DocumentPathResolution resolution = DocumentPathResolution.Of(
			solution,
			path,
			notFoundMessage ?? $"'{path}' is not a solution-compiled .cs, .razor or .cshtml document.");
		if (resolution.Error is not null)
			return (null, resolution.Error);

		RazorSourceDocument? source = await ResolveResolvedAsync(solution, resolution.Resolved!, cancellationToken);
		return source is null
			? (null, notFoundMessage is null ? null : Error.NotFound(notFoundMessage))
			: (source, null);
	}

	private static async Task<RazorSourceDocument?> ResolveResolvedAsync(Solution solution, ResolvedDocumentPath resolved, CancellationToken cancellationToken)
	{
		if (!IsRazorSourcePath(resolved.AbsolutePath))
		{
			Document? document = FindDocumentAt(solution, resolved.AbsolutePath);
			return document is null
				? null
				: new RazorSourceDocument([document], null, null, resolved.RelativePath, resolved.ResolvedBySuffix);
		}

		TextDocument? razor = FindAdditionalAt(solution, resolved.AbsolutePath);
		if (razor?.FilePath is not string razorPath)
			return null;

		IReadOnlyList<Document> generated = await FindGeneratedDocumentsAsync(solution.GetProject(razor.Project.Id)!, razorPath, cancellationToken);
		if (generated.Count == 0)
			return null;

		return new RazorSourceDocument(
			generated,
			razorPath,
			await razor.GetTextAsync(cancellationToken),
			resolved.RelativePath,
			resolved.ResolvedBySuffix);
	}

	/// <summary>The text positions are expressed against: the Razor source, or the .cs document's text.</summary>
	public async Task<SourceText> GetSourceTextAsync(CancellationToken cancellationToken = default) =>
		RazorText ?? await Document.GetTextAsync(cancellationToken);

	/// <summary>
	/// Maps a span in the source text (see <see cref="GetSourceTextAsync"/>) to the document holding it and the
	/// equivalent span there. Identity on <see cref="Document"/> for a .cs document; for Razor, both ends must
	/// fall inside C# the compiler copied from the Razor file (an <c>@code</c> block, an expression, a
	/// directive) within one generated document, or null.
	/// </summary>
	public async Task<RazorMappedSpan?> MapToDocumentAsync(TextSpan sourceSpan, CancellationToken cancellationToken = default)
	{
		if (RazorPath is null || RazorText is null)
			return new RazorMappedSpan(Document, sourceSpan);

		foreach (Document document in Documents)
		{
			if (await MapToGeneratedAsync(document, sourceSpan, cancellationToken) is TextSpan span)
				return new RazorMappedSpan(document, span);
		}

		return null;
	}

	/// <summary>The generated document named by <paramref name="fileName"/> (as <see cref="DocumentFileName"/> gives it), else <see cref="Document"/>.</summary>
	public Document DocumentNamed(string? fileName) =>
		fileName is null
			? Document
			: Documents.FirstOrDefault(document => string.Equals(DocumentFileName(document), fileName, StringComparison.OrdinalIgnoreCase)) ?? Document;

	/// <summary>The file name identifying one of <see cref="Documents"/>; null for an ordinary .cs document, which is its only one.</summary>
	public string? DocumentFileName(Document document) =>
		RazorPath is null ? null : Path.GetFileName(document.FilePath ?? document.Name);

	private async Task<TextSpan?> MapToGeneratedAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
	{
		SyntaxTree? tree = await document.GetSyntaxTreeAsync(cancellationToken);
		if (tree is null)
			return null;
		SourceText generatedText = await tree.GetTextAsync(cancellationToken);

		int? start = MapPosition(tree, generatedText, RazorPath!, RazorText!, sourceSpan.Start, cancellationToken);
		if (start is null)
			return null;
		if (sourceSpan.IsEmpty)
			return new TextSpan(start.Value, 0);

		int? end = MapPosition(tree, generatedText, RazorPath!, RazorText!, sourceSpan.End, cancellationToken);
		if (end is null || end.Value < start.Value)
			return null;

		return TextSpan.FromBounds(start.Value, end.Value);
	}

	/// <summary>True when <paramref name="location"/> in the generated document maps into this Razor file.</summary>
	public bool MapsToSource(Location location)
	{
		if (RazorPath is null)
			return true;

		FileLinePositionSpan mapped = location.GetMappedLineSpan();
		return mapped.HasMappedPath && MapsToSource(mapped.Path);
	}

	/// <summary>True when a #line-mapped <paramref name="path"/> is this Razor file (always true for a .cs document).</summary>
	public bool MapsToSource(string path) => RazorPath is null || SamePath(path, RazorPath);

	/// <summary>
	/// The Razor-generated documents for <paramref name="razorPath"/>: the ones the compiler named after it
	/// (<c>X_razor.g.cs</c> first, then <c>X_razor.decl.g.cs</c>), falling back to any one generated document
	/// whose #line directives map into it. Imports files such as <c>_Imports.razor</c> are mapped into every
	/// component, so the name match is preferred.
	/// </summary>
	private static async Task<IReadOnlyList<Document>> FindGeneratedDocumentsAsync(Project project, string razorPath, CancellationToken cancellationToken)
	{
		string expectedStem = Path.GetFileNameWithoutExtension(razorPath) + "_" + Path.GetExtension(razorPath).TrimStart('.');
		var named = new List<Document>();
		Document? fallback = null;

		foreach (Document document in project.Documents)
		{
			if (document.FilePath is not string path || !RazorMapping.IsRazorGeneratedPath(path))
				continue;

			SyntaxTree? tree = await document.GetSyntaxTreeAsync(cancellationToken);
			if (tree is null || !tree.GetLineMappings(cancellationToken).Any(mapping => !mapping.IsHidden && SamePath(mapping.MappedSpan.Path, razorPath)))
				continue;

			if (RazorMapping.RazorOutputStem(Path.GetFileName(path)) is string stem && stem.EndsWith(expectedStem, StringComparison.OrdinalIgnoreCase))
				named.Add(document);
			else
				fallback ??= document;
		}

		if (named.Count > 0)
		{
			// The render-tree document (X_razor.g.cs) stays the primary one, as it was before declarations split off.
			return [.. named.OrderBy(document => document.FilePath!.EndsWith(".decl.g.cs", StringComparison.OrdinalIgnoreCase) ? 1 : 0)];
		}

		return fallback is null ? [] : [fallback];
	}

	/// <summary>
	/// The generated offset whose mapped position is <paramref name="razorOffset"/>, found through the #line
	/// mapping that covers it. The compiler copies C# regions verbatim, so the offset within the mapped region
	/// carries over; each candidate is confirmed by mapping it forward again.
	/// </summary>
	private static int? MapPosition(SyntaxTree tree, SourceText generatedText, string razorPath, SourceText razorText, int razorOffset, CancellationToken cancellationToken)
	{
		LinePosition target = razorText.Lines.GetLinePosition(razorOffset);

		foreach (LineMapping mapping in tree.GetLineMappings(cancellationToken))
		{
			if (mapping.IsHidden || !SamePath(mapping.MappedSpan.Path, razorPath))
				continue;
			if (!TryGetOffset(razorText, mapping.MappedSpan.StartLinePosition, out int razorStart)
				|| !TryGetOffset(razorText, mapping.MappedSpan.EndLinePosition, out int razorEnd)
				|| razorOffset < razorStart || razorOffset > razorEnd)
				continue;

			LinePosition generatedStart = mapping.CharacterOffset is int characterOffset
				? new LinePosition(mapping.Span.Start.Line, characterOffset)
				: mapping.Span.Start;
			if (!TryGetOffset(generatedText, generatedStart, out int generatedStartOffset))
				continue;

			int candidate = generatedStartOffset + (razorOffset - razorStart);
			if (candidate > generatedText.Length)
				continue;

			FileLinePositionSpan roundTrip = tree.GetMappedLineSpan(new TextSpan(candidate, 0), cancellationToken);
			if (roundTrip.HasMappedPath && SamePath(roundTrip.Path, razorPath) && roundTrip.StartLinePosition == target)
				return candidate;
		}

		return null;
	}

	private static bool TryGetOffset(SourceText text, LinePosition position, out int offset)
	{
		offset = 0;
		if (position.Line < 0 || position.Line >= text.Lines.Count || position.Character < 0)
			return false;

		TextLine line = text.Lines[position.Line];
		offset = line.Start + position.Character;
		return offset <= line.EndIncludingLineBreak;
	}

	private static bool SamePath(string left, string right) =>
		string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

	private static string Normalize(string path) =>
		path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

	private static Document? FindDocumentAt(Solution solution, string fullPath) =>
		solution.GetDocumentIdsWithFilePath(fullPath)
			.Select(solution.GetDocument)
			.FirstOrDefault(document => document is not null);

	private static TextDocument? FindAdditionalAt(Solution solution, string fullPath) =>
		solution.GetDocumentIdsWithFilePath(fullPath)
			.Select(solution.GetAdditionalDocument)
			.FirstOrDefault(document => document is not null);
}

/// <summary>A span mapped from a source file into the document holding it (see <see cref="RazorSourceDocument.MapToDocumentAsync"/>).</summary>
public readonly record struct RazorMappedSpan(Document Document, TextSpan Span);
