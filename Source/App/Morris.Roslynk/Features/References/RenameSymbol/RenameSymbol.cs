using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.References.RenameSymbol;

public static partial class RenameSymbol
{
	[Operation("rename_symbol", Description = """
		Renames a symbol and references across partial classes, code-behind and loaded frameworks under
		shared conditional coverage. Strings/comments are unchanged. Razor supports @code, markup
		expressions and component attributes.

		Generated-only targets return NotSupported naming the generator; edit its input/generator.
		Partial members/types with handwritten declarations are renamable; generators follow their names.

		Additional header: resolvedSymbol (pre-rename name).
		Errors: Invalid for invalid identifiers; Ambiguous with match candidates or fuzzy suggestions
		when no match; Stale naming the target changed on disk/in the model since computation.
		Intervening edits to other files are preserved.
		""", Group = "edit")]
	[DisplayName("Rename a symbol")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description("""
		Symbol name.
		""")]
		public required string symbolName { get; set; }

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
