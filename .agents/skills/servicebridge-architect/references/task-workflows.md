# ServiceBridge Task Workflows

Load this reference only when the active task needs execution, debugging, review, prioritization, validation, or reporting details.

## Feature or refactor

1. Map the request to the ServiceBridge business flow and identify upstream/downstream dependencies.
2. Trace the existing implementation end-to-end: UI/API → Application → Domain → Infrastructure → database/tests.
3. Define the smallest production-safe PR-sized scope.
4. Implement in the correct layers with concrete command/query/service/repository names.
5. Add tests that lock business rules and prevent regressions.
6. Run focused tests first, then solution build and relevant full suites.
7. Review the diff for authorization, sensitive-data leakage, over-fetching, accidental contract changes, and unrelated edits.

## Bug or failing test

1. Reproduce or inspect the exact failure.
2. Trace the execution and persistence path.
3. Fix the root cause, not only the visible symptom.
4. Add a regression test before or with the fix when practical.
5. Validate adjacent flows sharing the changed code.

## Code review

Prioritize findings:

1. security, credentials, payment, authorization, data loss, concurrency
2. domain invariant/state-machine violation
3. persistence, schema, transaction, or query correctness
4. API contract leakage or breaking behavior
5. architectural hotspot, test gap, maintainability, performance

For every real finding include:

- severity
- exact project/file/symbol
- concrete ServiceBridge risk
- required correction
- regression test needed

Do not pad reviews with style-only preferences.

## Priority decision

Prefer the work that unblocks the current dependency chain. Treat uncommitted completed work, missing migrations, security gaps, and missing integration coverage on critical persistence behavior as immediate risks.

## Validation

1. Run the focused checks for the changed behavior first.
2. Run the solution build and the required broader test suite when the task permits.
3. Record exact commands, outcomes, and material warnings; never imply validation that did not run.
4. Stop and report when a required baseline is unreliable or a prerequisite cannot be verified.

## Testing selection

- Domain unit tests: invariants and transitions.
- Application unit tests: orchestration and controlled failures.
- EF/SQL integration tests: projections, constraints, transactions, encryption persistence, indexes, and rowversion.
- API integration tests: authentication, authorization, callbacks, status codes, and contracts.
- Use real SQL Server semantics for payment idempotency, unique constraints, order-number concurrency, and assignment concurrency. EF InMemory is insufficient for these cases.
- Never weaken or delete a valid test merely to make the suite pass.

## Final response format

Keep reports compact and factual:

1. `Recommended level: Low|Medium|High`
2. `Decision` — chosen design or root cause.
3. `Implemented` or `Findings` — concrete classes/files/behavior.
4. `Validation` — exact build/test results and material warnings.
5. `Risks / Next dependency` — only material remaining issues.
6. `Git status` — branch and clean/dirty state; include commit hash only if one exists.
