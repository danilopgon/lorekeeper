# 08 — Quality Strategy

## Principle

Test behaviour at the cheapest layer that provides confidence. Most tests live below E2E; real boundaries are exercised with real infrastructure.

## Testing pyramid and tools

| Level | Tools | Protects |
| --- | --- | --- |
| Domain unit | xUnit, FluentAssertions | Invariants, policies, value objects, ranking maths |
| Application unit | xUnit, NSubstitute | Command/query orchestration and failure paths |
| Angular unit/component | Vitest, Angular Testing Library | Signals, rendering, interaction and accessible behaviour |
| Backend integration | WebApplicationFactory, Testcontainers, PostgreSQL/pgvector | HTTP pipeline, EF mappings, migrations, FTS and vector queries |
| Contract | Committed OpenAPI document diff, generated TypeScript types and Zod schemas | Angular ↔ API compatibility |
| End-to-end | Playwright | Critical user journeys only |
| AI evaluations | Versioned dataset and reproducible repository runner(s) | Retrieval, fusion, reranking and grounded generation |

## Tooling baseline

### Angular 22

- pnpm as the frontend package manager under Node 24.15+.
- Tailwind from block 00, aligned to the durable tokens and visual direction in `DESIGN.md`.
- Vitest as the test runner.
- Angular Testing Library for behaviour-first component tests.
- Playwright for E2E and accessibility smoke checks.
- Angular ESLint, Prettier and TypeScript strict mode.
- Avoid shallow tests coupled to component internals.

### ASP.NET Core

- .NET 10 as the backend runtime target from block 00.
- xUnit for unit and integration suites.
- FluentAssertions for readable outcomes.
- NSubstitute only at real ports; do not mock domain objects or EF queries.
- `WebApplicationFactory` for the real ASP.NET pipeline.
- Testcontainers with the same PostgreSQL extensions used in production.
- `dotnet format`, .NET analyzers, nullable enabled and warnings treated as errors.

### AI experimentation

Production regression gates should remain runnable from committed repository tooling and fixtures. Python may be introduced under `ai/` for model-ecosystem experiments, analysis or benchmark utilities when it provides concrete value. It is not an active block 00 scaffold dependency, runtime or CI check.

If Python tooling is introduced:

- prefer `uv` for reproducible environments;
- use pytest for tooling behaviour worth protecting;
- keep experiment configuration explicit and versioned;
- consume the same committed eval corpus as the product where practical;
- export machine-readable results rather than relying on notebook-only evidence;
- do not require Python for unrelated application tests unless a promoted runtime dependency genuinely needs it.

### System quality

- GitHub Actions as the CI platform.
- Conventional Commits enforced in CI and locally once scaffolded, using commitlint plus Husky/lint-staged or equivalent JavaScript tooling.
- Docker Compose PostgreSQL 17 for local development; Testcontainers should match required PostgreSQL extensions for automated integration tests.
- OpenAPI generation and contract-diff gate; breaking-change detection when a public API version exists (see `06`).
- Dependabot/Renovate or equivalent dependency updates.
- Dependency and container vulnerability scanning in CI.
- OpenTelemetry for traces, metrics and structured logs.

## Test placement

```text
apps/web/src/app/features/**/__tests__/
apps/web/e2e/
services/api/tests/Unit/
services/api/tests/Integration/
ai/tests/                       # only when Python tooling exists
tests/contracts/
tests/e2e/
tests/evals/
```

Follow the repository's actual conventions if the framework scaffolding establishes a different colocated layout.

## Required scenarios

- happy path and validation failures;
- empty, loading, error and success UI states;
- authorization and ownership;
- concurrency, retries and idempotency where applicable;
- real migrations and database constraints;
- keyboard navigation and accessible names;
- AI output validation, missing evidence and provider failure;
- regression test for every fixed defect with meaningful recurrence risk.

## AI quality gates

| Gate | Default policy |
| --- | --- |
| Ingestion normalisation/chunking | Deterministic and blocking |
| Retrieval Recall@K / MRR | Blocking against committed thresholds |
| RRF/reranker comparison | Report regression; block beyond agreed tolerance |
| Grounding/citations | Blocking on deterministic fixtures where possible |
| Live model quality | Scheduled/manual unless provider is deterministic and affordable |
| Local-model promotion | Requires measured quality gain plus acceptable latency/resource impact |

Thresholds belong beside the eval dataset and must be based on a recorded baseline, not invented aspirational numbers.

For reranking, compare relevant alternatives on the same corpus: lexical, vector, fused and candidate-reranked retrieval. Record ranking quality and operational metrics together. A local Hugging Face/SentenceTransformers/PyTorch implementation is not production-ready merely because it runs; it must beat or justify itself against the simpler baseline.

## CI quality gates

```text
Frontend:
pnpm install --frozen-lockfile
pnpm run format:check
pnpm run lint
pnpm run test
pnpm run build

Backend:
dotnet restore
dotnet format --verify-no-changes
dotnet build --no-restore
dotnet test --no-build

System:
GitHub Actions workflow gates
Conventional Commit check
OpenAPI generation + diff
integration tests with Testcontainers
Docker Compose PostgreSQL 17 local development check when applicable
deterministic retrieval evals when introduced
Playwright critical-path suite from apps/web/e2e

Optional AI workbench, only after a later block introduces it:
uv sync --frozen
uv run pytest
[reproducible benchmark/eval command]
```

Adapt script names to the repository during scaffold, then replace this block with verified commands. Do not leave fictional commands in an active project. Do not make optional Python checks blocking until the workbench exists and the owning block defines which checks are required.

## Definition of Done

- [ ] Acceptance criteria have test or review evidence.
- [ ] Applicable format, lint, build and test gates pass.
- [ ] Integration tests exercise every changed external/persistence boundary.
- [ ] Committed OpenAPI document and generated TypeScript output are in sync.
- [ ] Retrieval/eval baselines do not regress beyond the agreed tolerance.
- [ ] Accessibility states and keyboard flow were checked for UI changes.
- [ ] Security, ownership, observability and failure behaviour were reviewed.
- [ ] Documentation represents the resulting system.
- [ ] Any unexecuted check or remaining risk is explicit.

## Definition of Ready — blocking SDD gate

A block may start only when:

- Its outcome, included/excluded scope and dependencies are explicit.
- Required product/domain decisions are resolved in the source-of-truth documents.
- Acceptance criteria cover successful behaviour and relevant boundaries/failures.
- Contracts and data invariants needed by the slice are specified.
- Verification approach, fixtures and applicable checks are identified.
- No placeholder or unresolved question remains on the block's critical path.

Record readiness evidence in `10-roadmap.md`. If any required decision is unresolved, mark that block **Blocked**, list the missing decision, target document and unblock condition. Do not invent answers or silently skip gates. Unrelated ready blocks may proceed. Implementation changes to specified behaviour require updating the specification first.

## Mandatory campaign-isolation evidence

Use at least two campaigns with overlapping NPC names. Test retrieval/citations, invalid cross-campaign references and ingestion/reindex/removal boundaries against real persistence. Test cache isolation if caching is implemented. No user/account scaffolding is needed for these checks.

## Evaluation sequencing

Create fixed questions and expected sources before block 05A. Measure the lexical baseline in 05A, the vector baseline in 05B and fusion/reranking in 06. Block 07 consolidates measured regression thresholds; it is not the first evaluation activity. Live-provider variability must not be labelled deterministic.

Offline Python experiments may help compare embeddings, rerankers or batching strategies, but the committed product baseline remains the source of truth. Any result used to justify a production dependency must be reproducible from versioned inputs and configuration.

## Initial workspace/ingestion approach

Add behavioural verification for campaign switches with late asynchronous responses, partial import failure and retry idempotency, atomic source replacement, failed-update preservation and removal/citation behaviour. Use the [UX scenarios](12-workspace-and-ingestion-ux.md) at the cheapest meaningful layer; cover critical complete flows with Playwright.
