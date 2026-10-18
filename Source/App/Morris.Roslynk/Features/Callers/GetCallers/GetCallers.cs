using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Callers.GetCallers;

public static partial class GetCallers
{
	[Operation("get_callers", Description = """
		SupportsPaging=Y

		Finds callers, including implicit calls: foreach/await enumeration and awaiter members
		(GetEnumerator/MoveNext/Current/Dispose, GetAwaiter/IsCompleted/GetResult, async equivalents),
		using/await using Dispose/DisposeAsync, deconstruction/positional patterns, collection Add,
		operators/conversions (+=/++/implicit/conditional true/false), query methods, interpolated-string
		handlers, implicit base(), and Length/Count for ^/.. . See get_callees for its narrower coverage.

		Body: project > folder > file > namespace > containing type; leaves name callers and declaration locations.
		""")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Method name.
		""")]
		public required string methodName { get; set; }

		[Description(ParameterDescriptions.StartItem)]
		[DefaultValue(1)]
		public int startItem { get; set; } = 1;

		[Description(ParameterDescriptions.MaxSize)]
		[DefaultValue(0)]
		public int maxSize { get; set; } = 0;
	}

	public sealed class Response : IOutlineResponse
	{
		public string Text { get; set; } = "";
	}

	internal sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
