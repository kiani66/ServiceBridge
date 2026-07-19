# ServiceBridge Repository Instructions

## Repository Skill

For every ServiceBridge or legacy PayBridge architecture, implementation,
debugging, review, testing, prioritization, branch, PR, or Codex task, invoke
`$servicebridge-architect` from
`.agents/skills/servicebridge-architect/SKILL.md`. Read that `SKILL.md` and any
task-relevant references it requires before analysis or implementation, and
follow its repository-first and change-control instructions together with this
file.

## Installed .NET Skills and Plugins

The Codex environment may expose specialized .NET Skill and Plugin families.
Before starting a task, inspect only the families relevant to that task and
verify their current availability and exact `SKILL.md` paths; a name in this
file is a selection hint, not proof that it is installed. If a relevant Skill
exists, read its complete `SKILL.md` before analysis or implementation and
follow its workflow, constraints, and verification requirements.

Use the smallest relevant set. Do not load unrelated Skills. For multi-stage
work, combine only the Skills needed for those stages. Discovery alone does not
count as use: report a Skill as used only when its `SKILL.md` was read and its
workflow materially guided the work.

Selection hints, subject to verified availability:

| Task | Skill or Plugin family |
| --- | --- |
| General .NET implementation | `dotnet` |
| EF Core or SQL Server data work | `dotnet-data` |
| Build or project-system issues | `dotnet-msbuild` |
| NuGet restore or package issues | `dotnet-nuget` |
| Unit or integration testing | `dotnet-test` |
| ASP.NET Core, API, or authentication work | `dotnet-aspnetcore` |
| Blazor UI work | `dotnet-blazor` |
| Runtime diagnostics or performance | `dotnet-diag` |
| AI features in .NET | `dotnet-ai` |

## Repository and Local Instructions

Before making changes, locate and read the instructions applicable to the
target files. Check, when present and relevant, `AGENTS.md`, `README.md`,
`CONTRIBUTING.md`, `docs/**`, `.agents/**`, `skills/**`, and `**/SKILL.md`.
Instructions closer to a modified file take precedence over broader ones;
local instructions supplement root instructions unless they explicitly
override them. Never invent the contents of a missing document. Report relevant
missing expected documentation instead of fabricating it.

## Project Source of Truth

The current source, project references, migrations, configuration, tests, Git
state, and runtime evidence are authoritative for what is implemented now.
Applicable repository documentation governs product, domain, and architecture
decisions. Before architecture, domain, database, payment, order, security,
fulfillment, or workflow changes, read the relevant documents that actually
exist under `docs/`, which may include:

- `docs/ServiceBridge.ProjectGuideline.md`
- `docs/ServiceBridge.Roadmap.md`
- `docs/ServiceBridge.Architecture.md`
- `docs/ServiceBridge.BusinessRules.md`

Do not silently contradict documented decisions or copy their detailed
business content into this file. Report relevant missing documents and material
conflicts. An explicit current user request may propose changing a documented
decision, but identify the conflict clearly before making that change.

## Codebase Memory MCP

Use `codebase-memory-mcp` as the primary structural discovery mechanism for
architecture and symbol discovery, dependency and change-impact analysis,
call-path tracing, and cross-layer feature work. Use it before broad recursive
searches or reading many unrelated files. If the repository is not indexed,
run `index_repository`; re-index only when the index is missing, stale, or
structurally inconsistent.

Use the relevant tools where appropriate:

- `get_architecture`
- `search_graph`
- `query_graph`
- `trace_path`
- `search_code`
- `get_code_snippet`

Verify important graph findings against the actual source files. MCP guides
discovery; it does not replace source inspection. It is not mandatory for a
small, explicitly scoped single-file edit. Never claim a call path, dependency,
implementation, or impact from assumptions alone.

## Evidence-First Analysis

Base conclusions and changes on the evidence relevant to the task, such as
source code, configuration, project files, package references, Git diff, build
and test output, runtime logs, stack traces, SQL queries and execution plans, EF
migrations, database constraints, and HTTP requests and responses.

Label conclusions clearly as `Confirmed finding`, `Likely cause`, or
`Unverified hypothesis`. Do not apply a speculative fix when the repository,
logs, build or test output, database evidence, or execution plan can be
inspected.

## ServiceBridge Architecture Constraints

- Keep the ASP.NET Core layered monolith; do not introduce MediatR,
  microservices, or a workflow engine.
- Commands mutate state. Queries return consumer-shaped DTOs. Never expose EF
  Core entities directly to API or UI.
- Avoid broad `Include` chains and over-fetching. Keep `DbContext` thin and use
  `IEntityTypeConfiguration<T>`.
- Use client-generated `Guid` IDs configured with `ValueGeneratedNever()`.
- Keep order pricing snapshots immutable after creation.
- Encrypt sensitive order credentials at rest and never log decrypted values.
- Keep order history and `AuditLog` append-only.
- Validate order state transitions in Domain. Use `ServiceResult<T>` for
  expected business failures.
- Keep Admin, Agent, payment, refund, and credential-view actions auditable.
- Verify trusted gateway proof before confirming payment and keep callbacks
  idempotent.

## Focused Change Policy

Make the smallest complete and correct change. Avoid unrelated refactoring,
broad renaming, unnecessary abstractions, new frameworks for isolated use
cases, formatting unrelated files, new dependencies without justification, and
architecture changes outside the task scope. Report unrelated issues separately
instead of silently expanding scope.

## Build and Test Verification

After code changes, run the closest relevant build, focused unit tests, relevant
integration tests, and solution-level build and tests when practical. Follow
the verification requirements of every selected Skill. Never claim an
unexecuted build or test passed.

If verification cannot run or fails, report the exact command, the reason, and
the behavior or risk left unverified. For SQL Server-specific transactions,
constraints, indexes, `rowversion`, concurrency, payment idempotency, encryption
persistence, and order-number generation, EF Core InMemory is not sufficient as
final verification.

Documentation-only changes do not require application build or test execution
unless another changed file or applicable repository instruction requires it.

## Final Response Requirements

Every implementation or review response must end with a clear report containing
these sections:

- `Summary`
- `Files changed`
- `Key implementation decisions`
- `Verification`
- `Remaining risks or follow-ups`

After that report, end with this exact format for every Skill actually used:

```text
Skills used:
- <exact skill name>
  SKILL.md: <exact path>
  Reason: <why this Skill was relevant and how its workflow was used>
```

List multiple Skills separately. Never report a Skill that was only discovered.
If no Skill was actually used, end exactly with:

```text
Skills used: none
Reason: no installed skill was sufficiently relevant to this task.
```
