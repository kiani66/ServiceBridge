## Codebase Memory MCP

## Repository Skill

For ServiceBridge or legacy PayBridge architecture, implementation, debugging,
review, testing, prioritization, branch, PR, and Codex tasks, invoke the
repository skill at `.agents/skills/servicebridge-architect/SKILL.md` as
`$servicebridge-architect`. Follow its repository-first and change-control
instructions in addition to the graph-discovery guidance below.

Use `codebase-memory-mcp` as the primary structural discovery tool for:

- architecture analysis
- symbol and implementation discovery
- call-path tracing
- dependency analysis
- change-impact analysis
- cross-layer feature work

Use the available tools where appropriate:

- `get_architecture`
- `search_graph`
- `query_graph`
- `trace_path`
- `search_code`
- `get_code_snippet`

Rules:

1. Use MCP before broad recursive searches or reading many unrelated files.
2. Verify important graph findings against the actual source files.
3. Do not treat graph output as a replacement for source-code verification.
4. Do not require MCP for small, explicitly scoped single-file changes.
5. Re-index only when the repository is missing, stale, or structurally inconsistent.
