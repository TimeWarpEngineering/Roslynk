# Glossary

Shared meanings for words this repository uses.

When an agent or reviewer has to ask what a term or acronym means, or sees one used two ways, the definition goes into `glossary.md` in the same PR.

## Terms

| Term | Meaning |
|------|---------|
| served app | The one TimeWarp.Nuru app `roslynk`: the CLI, the shared server `roslynk serve` (Nuru Daemon on a per-build Unix socket) and the MCP host `roslynk stdio`. Every operation call forwards to the server, so all callers share one warm engine. |
| engine | `Morris.Roslynk`: the Roslyn workspace, tools, operation contracts and shared infrastructure. It has no Nuru dependency; the app composes it on first use. |
| operation | One tool as a typed contract: a request class (`Query` or `Command`) carrying `[Operation]` with its name, description, group and title, plus a `Response` and its `Renderer`. The CLI route, the MCP tool and the capabilities entry all derive from it. |
| instance | A `RoslynInstance`: one loaded solution with its workspace, file watcher, write queue and published model, keyed in `InstanceRegistry` by its normalized solution path. |
| solutionId | The echoed handle for an open solution: its absolute path with forward slashes (`SolutionKey.SolutionId`). |
| model | A `SolutionModel`: the immutable published wrapper of status, optional Roslyn `Solution` and fault message that readers capture. |
| publication generation | The fresh GUID `Id` each new `SolutionModel` gets when published. It identifies a publication, not content: equal generations mean the same snapshot, different ones do not prove the text changed. |
| pinned snapshot | The one `SolutionModel` a read captures and uses throughout, so symbols and trees never mix two solutions. `multi_query` pins one for a whole batch; `expectSnapshot` rejects a continuation against a different generation. |
| write queue | The single-consumer channel on `RoslynInstance` that runs every write, watcher fold, rebuild publication and diagnostics request in order under the write lock. |
| fold | Applying a watched disk change to the current model in place (new text, added or removed document) instead of reloading the solution. |
| dirty rebuild | A lazy, coalesced reload of the solution from MSBuild, started on next use after a change the watcher cannot fold. |
| rebase | Replaying only the documents a write changed relative to its `basedOn` snapshot onto the latest solution inside the queue; a conflicting intervening change raises `StaleWriteException`. |
| stale | A write refused because disk text or an intervening publication no longer matches what the edit was computed from. Reported as `error=Stale` with `stale=` paths. |
| projection | One variant of the solution with a conditional-compilation symbol toggled (`ProjectionService`). Queries run per projection and deduplicate by `ProjectionService.KeyOf`, so inactive `#if` branches are covered one symbol at a time. |
| outline | The compact text protocol every tool returns: `key=value` headers, an optional blank line and a tab-indented body of positional, comma-separated leaves. |
| leaf | One outline body line: `kind,name,loc[,extra...]`, fields positional, several values in one field pipe-delimited, a value with a comma single-quoted. |
| response budget | The server-wide ceiling on one response's size (`Roslynk:MaxResponseChars`, default 80,000). Paging tools bound `maxSize` by it; `multi_query` delivers whole results smallest-first until it is full. |
| retry line | The `retry=<index>:<size>` header `multi_query` emits for operations that did not fit the budget, each size a ratio of the budget for planning continuations. |
| tracked analyzer | An analyzer DLL a load depends on (an unbuilt generator or one a project of the solution builds) whose file stamp is probed on use and swapped in when it changes. |
| RazorGenerationState | The per-workspace record of each Razor project's generator, driver, generated documents and Razor diagnostics, used to regenerate component C# after a write. |
| group | The purpose group on `[Operation(Group)]`: `symbols`, `references`, `solution`, `edit` or `batch`. It filters `--capabilities` and never prefixes the route. |

## Acronyms

| Acronym | Expansion |
|---------|-----------|
| MCP | Model Context Protocol: the tool protocol `roslynk stdio` serves. |
| CPM | Central Package Management: package versions live in `Directory.Packages.props`, not in project files. |
| TFM | Target Framework Moniker, such as `net10.0`; a multi-targeted project loads one Roslyn project per TFM. |
| OTLP | OpenTelemetry Protocol: the export the engine's traces and metrics use when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. |
