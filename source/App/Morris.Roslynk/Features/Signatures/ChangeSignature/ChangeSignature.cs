using System.ComponentModel;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using Morris.Roslynk.Infrastructure.Documentation;
using Morris.Roslynk.Infrastructure.Operations;

namespace Morris.Roslynk.Features.Signatures.ChangeSignature;

public static partial class ChangeSignature
{
	[Operation("change_signature", Description = """
		Adds an optional parameter to one ordinary method/local function.
		Method groups, nameof and cref references remain unchanged. Supports Razor @code declarations
		and markup calls.

		Errors: NotSupported for virtual/override/abstract, interface members/implementations, partial
		methods, params methods, constructors or generated-only declarations (names the generator);
		Invalid for invalid parameter name/type/default; Ambiguous for multiple overloads (candidates).

		Additional headers: resolvedMethod (pre-edit overload with parameter types), updatedCallSites.
		symbolName accepts a uniquely-matching dotted-segment suffix.
		""", Group = "edit")]
	[DisplayName("Add a parameter to a method")]
	public sealed class Command : ICommand<OneOf<Response, SharedProblemDetails>>
	{
		[Description(ParameterDescriptions.SolutionId)]
		public required string solutionId { get; set; }

		[Description(ParameterDescriptions.SymbolName)]
		public required string symbolName { get; set; }

		[Description("""
		New parameter type.
		""")]
		public required string parameterType { get; set; }

		[Description("""
		New parameter's C# identifier.
		""")]
		public required string parameterName { get; set; }

		[Description("""
		Required default expression, e.g. default, null, 0.
		""")]
		public required string defaultValue { get; set; }

		[Description("""
		Named argument expression for all call sites; omit to use the default.
		""")]
		[DefaultValue(null)]
		public string? callSiteArgument { get; set; } = null;

		[Description(ParameterDescriptions.CheckOnly)]
		[DefaultValue(false)]
		public bool checkOnly { get; set; } = false;
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
