using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Callers.GetCallees;

public static partial class GetCallees
{
	[Operation("get_callees", Description = """
		SupportsPaging=Y

		Lists methods, constructors, property/event accessors and operators called by a member.
		Property reads/writes name their accessors (compound/increment reports both); e += h names
		the add accessor. A property includes its accessors; a field includes its initializer;
		constructors include initializers they run. Includes user-defined operators/conversions,
		using's Dispose, delegate method groups, nested lambdas and local functions. Abstract/interface
		declarations and auto-properties without code call nothing.

		Implicit foreach (GetEnumerator, MoveNext, Current) and await (GetAwaiter, IsCompleted, GetResult)
		calls are excluded; get_callers includes them. Explicit calls remain included.

		Solution callees nest project > folder > file > namespace > type; metadata callees appear
		under <external:AssemblyName> with empty loc. Leaves: <memberKind>,<memberName>,<loc>, plus
		parameterTypes only for overloads declared in the callee's type (parameterless overload: '()'). Constructed generics collapse to their definitions; extension
		methods include their 'this' parameter.
		""", Group = "references")]
	[DisplayName("Get the members a member calls")]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Member name.
		""")]
		public required string memberName { get; set; }

		[Description("""
		Exclude metadata callees (BCL/NuGet).
		""")]
		[DefaultValue(false)]
		public bool excludeExternal { get; set; } = false;

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

	public sealed class Renderer : IResponseRenderer<Response>
	{
		public static readonly Renderer Instance = new();

		public string Render(Response response) => response.Text;
	}
}
