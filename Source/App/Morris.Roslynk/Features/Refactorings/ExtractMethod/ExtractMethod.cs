using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Refactorings.ExtractMethod;

public static partial class ExtractMethod
{
	[Operation("extract_method", Description = """
		Extracts contiguous C# statements or one expression into a method/local function, replacing the
		selection with a call. Roslyn data-flow determines parameters, ref/out, return value, static/async
		and call-site code. Surrounding whitespace is ignored.

		Additional headers: method (final name), symbolName (usable in name-based tools, e.g.
		N.T.M(int).NewMethod(string) for a local function), kind (Method|LocalFunction), signature
		(declaration header), call (statement containing the call).

		Errors:
		- NotSupported: extraction unavailable, behavior-change warnings, new compile errors (listed),
		  or generated .g.cs targets (including Razor output).
		- Conflict: methodName would bind a call to another member.
		- NotFound: unloaded source file.
		- Invalid: empty/out-of-range selection or invalid methodName.

		Razor selections outside C# return NotSupported; inserting a method outside @code may be unmappable.
		""")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.DocumentPath)]
		public required string documentPath { get; set; }

		[Description("""
		Selection start line.
		""")]
		public required int startLine { get; set; }

		[Description("""
		Selection start column.
		""")]
		public required int startColumn { get; set; }

		[Description("""
		Selection end line.
		""")]
		public required int endLine { get; set; }

		[Description("""
		Selection end column.
		""")]
		public required int endColumn { get; set; }

		[Description("""
		Extracted method name; omit to keep Roslyn's chosen name.
		""")]
		[DefaultValue(null)]
		public string? methodName { get; set; } = null;

		[Description("""
		Extract as a local function.
		""")]
		[DefaultValue(false)]
		public bool asLocalFunction { get; set; } = false;

		[Description("""
		Preview code and changed paths without writing.
		""")]
		[DefaultValue(false)]
		public bool checkOnly { get; set; } = false;
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
