using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Morris.Roslynk.Features.Patching.ApplyPatch;

/// <summary>
/// The version a patched file was based on: the path as it appears in the patch header (or its absolute
/// path) and the <c>documentVersion</c> hash from when it was read. The apply re-checks the file on disk
/// against this hash and refuses if it moved. The JSON names are lowercase, as tool arguments are.
/// </summary>
public sealed class FileVersion
{
	[JsonPropertyName("path")]
	[Description("Path as in the patch header, or absolute.")]
	public string Path { get; }

	[JsonPropertyName("version")]
	[Description("documentVersion hash from when the file was read.")]
	public string Version { get; }

	public FileVersion(string path, string version)
	{
		Path = path;
		Version = version;
	}
}
