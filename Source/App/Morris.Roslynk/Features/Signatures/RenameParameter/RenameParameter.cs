using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Signatures.RenameParameter;

public static partial class RenameParameter
{
	[Operation("rename_parameter", Description = """
		Renames one method/local function/constructor/indexer overload's parameter, updating declaration,
		body uses, named call arguments and <paramref> docs. Strings/comments/unrelated text are unchanged.

		Cascades through overrides, interfaces/implementations and partial-method parts when a source
		parameter at the same position has the old name. Other names, metadata and generated-only
		related declarations remain unchanged, listed in unchangedRelated.

		Additional headers: resolvedMethod (pre-rename member), parameter (old name), renamedMembers
		(declaration count including target), unchangedRelated (semicolon-separated; omitted if empty).

		Errors:
		- Invalid: invalid identifier or newName=parameterName.
		- Ambiguous methodId: overload candidates; no match gives fuzzy suggestions.
		- NotFound: absent parameterName; lists parameters.
		- Conflict: newName clashes with a parameter, local, local function, lambda/query variable or
		  type parameter in a renamed declaration.
		- NotSupported: unsupported target kind, positional record parameter, metadata-only or
		  generated-only target (names generator).
		""", Group = "edit")]
	[DisplayName("Rename a method parameter")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Declaring method/local function/constructor/indexer name.
		""")]
		public required string methodId { get; set; }

		[Description("""
		Current parameter name.
		""")]
		public required string parameterName { get; set; }

		[Description("""
		New C# identifier.
		""")]
		public required string newName { get; set; }

		[Description(ParameterDescriptions.CheckOnly)]
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
