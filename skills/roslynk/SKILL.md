---
name: roslynk
description: Use Roslynk for semantic inspection and edits of *.cs, *.razor and *.cshtml solution files.
---

Follow server instructions and tool schemas; without the MCP tools, run the same tool as `roslynk <tool> --<parameter> <value>` (`roslynk --capabilities` lists them; object and array arguments go in `--json-args`). Both share one warm server; a failed call is an error result whose text is the `error=` outline (on the CLI: stderr and a non-zero exit code).
