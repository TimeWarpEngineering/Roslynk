namespace Morris.Roslynk.Infrastructure.Operations;

/// <summary>
/// A response whose MCP text is the outline the tool already built.
/// The renderer writes <see cref="Text"/> unchanged.
/// </summary>
internal interface IOutlineResponse
{
	string Text { get; set; }
}
