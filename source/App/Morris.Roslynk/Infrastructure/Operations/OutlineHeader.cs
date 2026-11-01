namespace Morris.Roslynk.Infrastructure.Operations;

/// <summary>
/// One <c>key=value</c> header from an error outline, in emission order.
/// </summary>
internal sealed class OutlineHeader
{
	public required string Key { get; init; }
	public required string Value { get; init; }
}
