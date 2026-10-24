using System.Text;
using TimeWarp.Foundation.Types;

namespace Morris.Roslynk.Infrastructure.Operations;

/// <summary>
/// Reads and writes the header-only <c>error=</c> outline as a <see cref="SharedProblemDetails"/>.
/// <see cref="SharedProblemDetails.Type"/> is the error code, <see cref="SharedProblemDetails.Detail"/> is the
/// message, and <c>candidates</c>, <c>staleFiles</c> and <c>rejected</c> live in
/// <see cref="SharedProblemDetails.Extensions"/>. The ordered headers are kept too, so a problem parsed from
/// today's outline renders back to the same bytes, including keys such as <c>rejected</c> and <c>snapshot</c>.
/// </summary>
internal static class ProblemOutline
{
	public const string CandidatesKey = "candidates";
	public const string StaleFilesKey = "staleFiles";
	public const string RejectedKey = "rejected";
	public const string SolutionStatusKey = "solutionStatus";
	public const string HeadersKey = "headers";
	public const string BodyKey = "body";

	public static bool TryParse(string text, out SharedProblemDetails? problem)
	{
		problem = null;
		if (string.IsNullOrEmpty(text) || !text.StartsWith("error=", StringComparison.Ordinal))
			return false;

		var headers = new List<OutlineHeader>();
		string? body = null;
		string rest = text;
		while (rest.Length > 0)
		{
			int newline = rest.IndexOf('\n');
			string line = newline < 0 ? rest : rest[..newline];
			string next = newline < 0 ? "" : rest[(newline + 1)..];
			if (line.Length == 0)
			{
				body = next;
				break;
			}

			int equals = line.IndexOf('=');
			if (equals <= 0)
				return false;

			headers.Add(new OutlineHeader { Key = line[..equals], Value = line[(equals + 1)..] });
			if (newline < 0)
				break;
			rest = next;
		}

		if (headers.Count == 0 || headers[0].Key != "error")
			return false;

		var parsed = new SharedProblemDetails
		{
			Type = headers[0].Value,
			Detail = headers.FirstOrDefault(header => header.Key == "errorMessage")?.Value,
		};

		List<string> candidates = headers.Where(header => header.Key == "candidate").Select(header => header.Value).ToList();
		List<string> staleFiles = headers.Where(header => header.Key == "stale").Select(header => header.Value).ToList();
		List<string> rejected = headers.Where(header => header.Key == "rejected").Select(header => header.Value).ToList();
		OutlineHeader? status = headers.FirstOrDefault(header => header.Key == "status");

		if (candidates.Count > 0)
			parsed.Extensions[CandidatesKey] = candidates;
		if (staleFiles.Count > 0)
			parsed.Extensions[StaleFilesKey] = staleFiles;
		if (rejected.Count > 0)
			parsed.Extensions[RejectedKey] = rejected;
		if (status is not null)
			parsed.Extensions[SolutionStatusKey] = status.Value;

		parsed.Extensions[HeadersKey] = headers;
		if (body is not null)
			parsed.Extensions[BodyKey] = body;

		problem = parsed;
		return true;
	}

	public static string Format(SharedProblemDetails problem)
	{
		if (problem.Extensions.TryGetValue(HeadersKey, out object? raw) && raw is IEnumerable<OutlineHeader> headers)
		{
			var builder = new StringBuilder();
			foreach (OutlineHeader header in headers)
				builder.Append(header.Key).Append('=').Append(header.Value).Append('\n');

			if (problem.Extensions.TryGetValue(BodyKey, out object? body) && body is string bodyText)
				builder.Append('\n').Append(bodyText);

			return builder.ToString();
		}

		var fallback = new StringBuilder();
		fallback.Append("error=").Append(problem.Type).Append('\n');
		fallback.Append("errorMessage=").Append(problem.Detail).Append('\n');
		AppendEach(fallback, "candidate", problem.Extensions, CandidatesKey);
		AppendEach(fallback, "stale", problem.Extensions, StaleFilesKey);
		AppendEach(fallback, "rejected", problem.Extensions, RejectedKey);
		if (problem.Extensions.TryGetValue(SolutionStatusKey, out object? status) && status is string statusText && statusText.Length > 0)
			fallback.Append("status=").Append(statusText).Append('\n');
		return fallback.ToString();
	}

	private static void AppendEach(StringBuilder builder, string headerKey, IDictionary<string, object?> extensions, string extensionKey)
	{
		if (!extensions.TryGetValue(extensionKey, out object? raw) || raw is not IEnumerable<string> values)
			return;

		foreach (string value in values)
			builder.Append(headerKey).Append('=').Append(value).Append('\n');
	}
}
