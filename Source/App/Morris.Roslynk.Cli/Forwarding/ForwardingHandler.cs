using Morris.Roslynk.Cli.Output;
using Morris.Roslynk.Infrastructure.Operations;
using OneOf;
using TimeWarp.Foundation.Types;
using TimeWarp.Mediator;
using TimeWarp.Terminal;

namespace Morris.Roslynk.Cli.Forwarding;

/// <summary>
/// The CLI's handler for every operation: sends the bound request to the shared daemon as the MCP
/// <c>tools/call</c> an agent would make (same tool name, same argument names) and turns the text that
/// comes back into the operation's outcome. An <c>error=</c> outline becomes a
/// <see cref="SharedProblemDetails"/> whose <see cref="SharedProblemDetails.Status"/> is the exit code from
/// <see cref="ExitCodes"/>; any other text is the response. Either way the exact daemon text is handed to
/// the <see cref="VerbatimTerminal"/>, so stdout or stderr carries the MCP bytes rather than Nuru's line
/// rendering.
/// </summary>
internal abstract class ForwardingHandler<TRequest, TResponse>
	where TRequest : class
	where TResponse : class, IOutlineResponse, new()
{
	private readonly ITerminal Terminal;

	protected ForwardingHandler(ITerminal terminal)
	{
		Terminal = terminal;
	}

	protected async Task<OneOf<TResponse, SharedProblemDetails>> ForwardAsync(TRequest request, CancellationToken cancellationToken)
	{
		ForwardedCall call = ForwardedCall.Of(request);
		ForwardedText text = await DaemonForwarder.CallAsync(call, cancellationToken);
		VerbatimTerminal? verbatim = Terminal as VerbatimTerminal;

		if (text.IsTransportFailure)
		{
			var unavailable = new SharedProblemDetails
			{
				Type = ExitCodes.UnavailableType,
				Detail = text.Text,
				Status = ExitCodes.Unavailable,
			};
			verbatim?.ExpectProblem(unavailable, text.Text + "\n");
			return unavailable;
		}

		if (ProblemOutline.TryParse(text.Text, out SharedProblemDetails? problem) && problem is not null)
		{
			problem.Status = ExitCodes.For(problem.Type);
			verbatim?.ExpectProblem(problem, text.Text);
			return problem;
		}

		if (text.IsError)
		{
			// An error result that is not an outline (no daemon tool produces one today): keep the text and
			// report it as a fault rather than as success.
			var fault = new SharedProblemDetails { Type = "Faulted", Detail = text.Text, Status = ExitCodes.For("Faulted") };
			verbatim?.ExpectProblem(fault, text.Text);
			return fault;
		}

		verbatim?.ExpectOutput(text.Text);
		return new TResponse { Text = text.Text };
	}
}

/// <summary>The forwarding handler for a query operation.</summary>
internal abstract class ForwardingQueryHandler<TQuery, TResponse>
	: ForwardingHandler<TQuery, TResponse>, IQueryHandler<TQuery, OneOf<TResponse, SharedProblemDetails>>
	where TQuery : class, IQuery<OneOf<TResponse, SharedProblemDetails>>
	where TResponse : class, IOutlineResponse, new()
{
	protected ForwardingQueryHandler(ITerminal terminal)
		: base(terminal)
	{
	}

	public Task<OneOf<TResponse, SharedProblemDetails>> Handle(TQuery query, CancellationToken cancellationToken) =>
		ForwardAsync(query, cancellationToken);
}

/// <summary>The forwarding handler for a command (write) operation.</summary>
internal abstract class ForwardingCommandHandler<TCommand, TResponse>
	: ForwardingHandler<TCommand, TResponse>, ICommandHandler<TCommand, OneOf<TResponse, SharedProblemDetails>>
	where TCommand : class, ICommand<OneOf<TResponse, SharedProblemDetails>>
	where TResponse : class, IOutlineResponse, new()
{
	protected ForwardingCommandHandler(ITerminal terminal)
		: base(terminal)
	{
	}

	public Task<OneOf<TResponse, SharedProblemDetails>> Handle(TCommand command, CancellationToken cancellationToken) =>
		ForwardAsync(command, cancellationToken);
}
