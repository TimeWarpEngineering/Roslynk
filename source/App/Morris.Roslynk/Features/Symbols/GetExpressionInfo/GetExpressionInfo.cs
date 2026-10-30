using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Symbols.GetExpressionInfo;

public static partial class GetExpressionInfo
{
	[Operation("get_expression_info", Description = """
		Returns compiler facts at a source position. Member/method names report the whole access/invocation: return type and
		bound overload.

		Header-only fields:
		- expr: expression text, truncated at 200 characters with '...'; exprKind, path, loc.
		- type, convertedType: '?' marks nullable annotations.
		- nullability: annotation/flowState (NotAnnotated/NotNull, Annotated/MaybeNull;
		  None/None when analysis is disabled).
		- conversion: e.g. 'implicit numeric', 'implicit user-defined <method>', 'identity'.
		- constant: C# literal or 'null'.
		- symbol, symbolKind: bound symbol; type/member names work in name-based tools;
		  locals/parameters/range variables use bare names.
		- instantiation: constructed generics/reduced extension methods.
		- origin: source|metadata; source adds symbolPath/symbolLoc, metadata adds assembly.
		- doc: XML summary when available.

		Unavailable facts use '{Unavailable}': type for method groups/namespaces, constant for
		non-constants, symbol for unbound expressions. Binding candidates add candidateReason
		and pipe-delimited candidates.

		Declared names (locals, parameters, fields, properties, methods, types, foreach/pattern variables)
		add declaration=Y: expr=name, exprKind=declaration syntax, type=declared type (method return type),
		nullability=declared annotation/initializer flow (None without initializer).

		Whitespace/comments/keywords/punctuation or files outside the solution return NotFound.
		filePath accepts absolute, solution-relative, or a unique path suffix/file name; a fallback match
		adds resolvedPath=<solution-relative path>. Several matching files return Ambiguous with those paths
		as candidates; an unloaded file returns NotFound.
		""", Group = "symbols", Title = "Get compiler facts about an expression")]
	[Destructive(false)]
	[OpenWorld(false)]
	public sealed class Query : IQuery<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.FilePath)]
		public required string filePath { get; set; }

		[Description("""
		Expression/declaration line.
		""")]
		public required int line { get; set; }

		[Description("""
		Expression/declaration column.
		""")]
		public required int column { get; set; }
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
