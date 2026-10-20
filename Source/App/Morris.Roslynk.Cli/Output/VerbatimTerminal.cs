using System.Text;
using TimeWarp.Foundation.Types;
using TimeWarp.Terminal;

namespace Morris.Roslynk.Cli.Output;

/// <summary>
/// The CLI's terminal: the system terminal, except that the daemon text of a forwarded call is written
/// exactly as the MCP result carried it. Nuru 4.0.0-beta.3 renders a success with <c>WriteLine</c> (one
/// added newline) and a problem as <c>"{Type} {Title}"</c> plus <c>Detail</c>; the forwarding handler
/// announces the text it returned, and the one Nuru write that renders it is replaced by the verbatim
/// bytes (the problem's detail line is dropped). Every other write passes through unchanged. This goes
/// away when Nuru renders verbatim and lets a renderer own the problem text (timewarp-nuru 508-011).
/// </summary>
internal sealed class VerbatimTerminal : ITerminal
{
	private readonly ITerminal Inner;
	private string? PendingOutput;
	private string? PendingProblemHeader;
	private string? PendingProblemText;
	private string? PendingProblemDetail;

	public VerbatimTerminal(ITerminal inner)
	{
		Inner = inner;
	}

	/// <summary>The next <c>WriteLine</c> of exactly <paramref name="text"/> writes it without a newline.</summary>
	public void ExpectOutput(string text) => PendingOutput = text;

	/// <summary>
	/// Nuru's rendering of <paramref name="problem"/> on stderr is replaced by <paramref name="text"/>.
	/// </summary>
	public void ExpectProblem(SharedProblemDetails problem, string text)
	{
		PendingProblemHeader = $"{problem.Type} {problem.Title}";
		PendingProblemText = text;
		PendingProblemDetail = string.IsNullOrEmpty(problem.Detail) ? null : problem.Detail;
	}

	private bool TryWriteOutput(string? message)
	{
		if (PendingOutput is null || !string.Equals(message ?? "", PendingOutput, StringComparison.Ordinal))
			return false;

		PendingOutput = null;
		if (message is { Length: > 0 })
			Inner.Write(message);
		Inner.Out.Flush();
		return true;
	}

	private bool TryWriteProblem(string? message)
	{
		if (PendingProblemHeader is not null && string.Equals(message, PendingProblemHeader, StringComparison.Ordinal))
		{
			PendingProblemHeader = null;
			Inner.Error.Write(PendingProblemText);
			Inner.Error.Flush();
			PendingProblemText = null;
			return true;
		}

		if (PendingProblemHeader is null && PendingProblemDetail is not null && string.Equals(message, PendingProblemDetail, StringComparison.Ordinal))
		{
			PendingProblemDetail = null;
			return true;
		}

		return false;
	}

	public ITerminal WriteLine(string? message = null)
	{
		if (!TryWriteOutput(message))
			Inner.WriteLine(message);
		return this;
	}

	public Task WriteLineAsync(string? message = null) =>
		TryWriteOutput(message) ? Task.CompletedTask : Inner.WriteLineAsync(message);

	public ITerminal WriteErrorLine(string? message = null)
	{
		if (!TryWriteProblem(message))
			Inner.WriteErrorLine(message);
		return this;
	}

	public Task WriteErrorLineAsync(string? message = null) =>
		TryWriteProblem(message) ? Task.CompletedTask : Inner.WriteErrorLineAsync(message);

	public ITerminal Write(string message)
	{
		Inner.Write(message);
		return this;
	}

	IConsole IConsole.Write(string message) => Write(message);

	IConsole IConsole.WriteLine(string? message) => WriteLine(message);

	IConsole IConsole.WriteErrorLine(string? message) => WriteErrorLine(message);

	public int CursorLeft { get => Inner.CursorLeft; set => Inner.CursorLeft = value; }

	public int CursorTop { get => Inner.CursorTop; set => Inner.CursorTop = value; }

	public bool CursorVisible { get => Inner.CursorVisible; set => Inner.CursorVisible = value; }

	public int WindowWidth => Inner.WindowWidth;

	public int WindowHeight => Inner.WindowHeight;

	public int BufferWidth => Inner.BufferWidth;

	public int BufferHeight => Inner.BufferHeight;

	public bool IsInteractive => Inner.IsInteractive;

	public bool SupportsColor => Inner.SupportsColor;

	public bool SupportsHyperlinks => Inner.SupportsHyperlinks;

	public ConsoleColor ForegroundColor { get => Inner.ForegroundColor; set => Inner.ForegroundColor = value; }

	public ConsoleColor BackgroundColor { get => Inner.BackgroundColor; set => Inner.BackgroundColor = value; }

	public bool TreatControlCAsInput { get => Inner.TreatControlCAsInput; set => Inner.TreatControlCAsInput = value; }

	public string Title { get => Inner.Title; set => Inner.Title = value; }

	public bool KeyAvailable => Inner.KeyAvailable;

	public event ConsoleCancelEventHandler? CancelKeyPress
	{
		add => Inner.CancelKeyPress += value;
		remove => Inner.CancelKeyPress -= value;
	}

	public ConsoleKeyInfo ReadKey() => Inner.ReadKey();

	public ConsoleKeyInfo ReadKey(bool intercept) => Inner.ReadKey(intercept);

	public void SetCursorPosition(int left, int top) => Inner.SetCursorPosition(left, top);

	public (int Left, int Top) GetCursorPosition() => Inner.GetCursorPosition();

	public void ResetColor() => Inner.ResetColor();

	public void Clear() => Inner.Clear();

	public void Beep() => Inner.Beep();

	public void Beep(int frequency, int duration) => Inner.Beep(frequency, duration);

	public Encoding InputEncoding { get => Inner.InputEncoding; set => Inner.InputEncoding = value; }

	public Encoding OutputEncoding { get => Inner.OutputEncoding; set => Inner.OutputEncoding = value; }

	public bool IsInputRedirected => Inner.IsInputRedirected;

	public bool IsOutputRedirected => Inner.IsOutputRedirected;

	public bool IsErrorRedirected => Inner.IsErrorRedirected;

	public TextReader In => Inner.In;

	public TextWriter Out => Inner.Out;

	public TextWriter Error => Inner.Error;

	public string? ReadLine() => Inner.ReadLine();

	public int Read() => Inner.Read();

	public Stream OpenStandardInput() => Inner.OpenStandardInput();

	public Stream OpenStandardOutput() => Inner.OpenStandardOutput();

	public Stream OpenStandardError() => Inner.OpenStandardError();

	public void SetIn(TextReader reader) => Inner.SetIn(reader);

	public void SetOut(TextWriter writer) => Inner.SetOut(writer);

	public void SetError(TextWriter writer) => Inner.SetError(writer);
}
