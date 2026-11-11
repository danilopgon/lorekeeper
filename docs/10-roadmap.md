# 10 — Implementation Roadmap

## Status and execution rule

`Not started` · `In progress` · `Blocked` · `Done`.

Apply the blocking Definition of Ready in `08-quality-strategy.md` before every block. Dependencies are necessary but not sufficient: resolve the entry decisions below in their owning documents. Record status and readiness evidence here; the work plan lives in the block's GitHub issue and verification evidence in each PR (see `conventions/documentation.md`). Unknowns on another block's path do not block independent ready work.

## Current focus

- **Done:** Definition, 00, 01, DS, 02.
- **Next:** the #18 refactor (ADR-008): Campaign moves to a command repository and a read interface, with unchanged behaviour. It is ready and sets the persistence pattern Sources reuses.
- **Then:** 04A Sources, ready since 2026-11-11 (see its readiness record); it starts after #18.
- **Execution order after that:** 05A → 03 → 05B. 03 runs after 05A because the first useful version needs no AI.
- **First useful version target:** 05A — the operator can import Markdown/text notes into a campaign and search them with source-backed fragments, without an LLM.

## Sequence

Block IDs are kept for traceability with issues and history; the order column is the execution order. Revised 2026-10-29 (see [revision notes](#revision-2026-10-29)).

| Order | Block | Outcome | Depends on | Required decisions before implementation | Exit evidence | Status |
| ---: | --- | --- | --- | --- | --- | --- |
| — | Definition | Product and scope | none | Personal multicampaign boundaries, first slice, non-goals and observable acceptance in PRODUCT, 00–03 | First slice passes Definition of Ready | Done |
| — | 00 | Skeleton and CI | Definition | Runtime/package versions, layout, E2E location, commands and CI tools in 04/08/09 | Scaffold builds; checks run; AGENTS commands updated | Done |
| — | 01 | Angular → .NET → PostgreSQL slice | 00 | Campaign schema/invariants, URL campaign selection, DTOs/errors, minimal UI states in 02/03/06/DESIGN | First flow and database isolation tests pass | Done |
| 1 | DS | Frontend design-system foundation (#8) | 01 | Approved in #8: self-hosted fonts and delivery (2026-10-01); Hallmark audit outcomes (2026-10-30); Spanish UI copy (ADR-005) | Fonts load locally; tokens generate Tailwind v4 utilities; current screens use shared primitives and states; Spanish UI with `lang="es"`; accessibility review recorded | Done |
| 1 | 02 | Generated OpenAPI contract | 01 | Recorded in 06 and ADR-006 (2026-10-31): build-time OpenAPI document, Development-only Swagger UI, hey-api types and Zod schemas, committed output and contract-diff policy; frontend query/command split | OpenAPI document and TypeScript output generated and committed; CI contract-diff gate fails on stale output; Swagger UI and `/openapi` absent outside Development; campaign feature uses `httpResource` queries and `CampaignCommands` with generated schemas | Done |
| 2 | #18 | Campaign persistence boundary | 01 | Recorded in ADR-008 (2026-11-11) | Campaign commands use `ICampaignRepository` and queries a read interface; Application references neither EF Core nor Npgsql; Campaign routes, payloads and error codes unchanged | Not started |
| 3 | 04A | Sources: Markdown/text and pasted text | 01, 02, DS, #18 | Recorded on 2026-11-11 in 02 (US-02), 03, 06, 07, 12 and ADR-008 to ADR-011 | Supported inputs imported per campaign; partial failure, update, removal and campaign isolation verified against PostgreSQL; job queue and SSE verified; OpenTelemetry traces visible in the Aspire dashboard; initial eval fixtures committed | Not started |
| 4 | 05A | Lexical source search (FTS) | 04A | FTS language/configuration, ranking and snippet/locator contract, whether indexing is a separate stage, removal versus in-flight query, the inspect action moved from 04A, search placement in the workspace (12/DESIGN), initial eval corpus with expected sources | Campaign-scoped search returns fragments with real locators; lexical baseline (Recall@K/MRR) recorded; no cross-campaign leakage | Not started |
| 5 | 03 | AI ports and deterministic fake | 01 | Recorded in 05 (2026-11-11); concrete models chosen during compatibility verification | Fakes verify orchestration; Ollama adapter compatibility checked; models recorded in 05 | Not started |
| 6 | 05B | Vector retrieval | 03, 05A | Embedding model/dimensions within pgvector index limits, vector index configuration, candidate limits; whether offline Python comparisons add value; NaN adoption | Vector baseline recorded beside the lexical one on the same corpus; no campaign leakage | Not started |
| 7 | 06 | Fusion and grounded answer (Chat) | 05B | RRF settings, reranker decision and `IReranker`, budgets, citations and historical citation resolution, contradictions, insufficient evidence, SSE chat event schema (ADR-010) and generation criteria in 02/05/06; hosted-provider data decision in 07; conversation persistence and drafts in 12 | Answers trace to campaign evidence; comparison against baselines recorded | Not started |
| 8 | 04B | Notion ingestion | 04A | Root/credentials, traversal, supported blocks, sync and remote deletion in 03/05/06/07/12 | Notion uses the same canonical pipeline and source lifecycle; campaign isolation verified | Not started |
| 9 | 07 | Regression gates | 06 | Measured thresholds/tolerances, frozen versus live eval execution in 08; whether Langfuse or equivalent adds value beyond OpenTelemetry | Repeatable gates with documented baseline and cost/variance policy | Not started |
| 10 | 08 | Complete flow and deployment | 07 | Hosting, Cloudflare Access/Tunnel details, backups, limits and rollback in 07/09 and ADR-002 | Critical flow passes; any remote exposure passes Access/origin-bypass smoke verification | Not started |

Rows sharing an order number may run in parallel. 04B may be scheduled any time after 04A; it is placed after 06 because Markdown exports already cover Notion content for the first useful version, and it should reuse a pipeline proven by 05A.

ADR-003 records MediatR as the narrow Campaign Application dispatch boundary. ADR-004 refines its HTTP adapter choice. ADR-008 adds module-specific repositories and read interfaces. None of them marks a future block ready or introduces pipeline behaviors, generic repositories or generic controller abstractions.

## Revision 2026-11-11

Decided in a maintainer interview on 2026-11-11.

| Change | Reason |
| --- | --- |
| #18 becomes a step before 04A | The maintainer chose one persistence pattern for Campaign and Sources (ADR-008). Applying it first to the small Campaign module sets the pattern before Sources needs its transaction. |
| 03 moves after 05A | The first useful version (05A) needs no AI. 03 is first required by 05B and develops against a local Ollama. |
| 06 no longer waits on #21 | ADR-010 adopts SSE for every server-to-browser stream; the chat event schema remains a 06 entry decision. |
| 04A runs background jobs, SSE and OpenTelemetry from the start | The maintainer chose jobs over synchronous processing so 05B reuses them (ADR-009); this fires ADR-007's review trigger. |

## Revision 2026-10-29

Changes are justified by dependencies, product value, effort and risk; architecture decisions ADR-001–004 are unchanged.

| Change | Reason |
| --- | --- |
| DS (#8) recorded as a block, kept as the gate for new UI | Typography is not loaded, tokens generate no utilities and screens mix two styling approaches. Building Sources on that baseline means rework, and the Spanish copy migration is cheapest before more screens exist. Delivery reduced from five chained PRs to three slices with unchanged acceptance: fonts and tokens; shared primitives and route states; Spanish UI and closure. |
| 02 runs in parallel with DS | It is backend/contract work with no UI dependency. The API has no OpenAPI generation yet, so the block starts by adding it. |
| 04A depends on 01, 02 and DS instead of 03 | Reading, chunking and storing Markdown needs no AI port; embeddings belong to 05B. The previous dependency delayed the first useful capability behind unrelated work. |
| 05 split into 05A (FTS) and 05B (vector) | Lexical search over imported notes is useful without an LLM and produces the lexical baseline 05 already required. Accepted by the maintainer on 2026-10-29; its UI placement is a 05A entry decision. |
| 03 can run in parallel with 04A/05A | It depends only on 01 and is required first by 05B. |
| 04B no longer blocks retrieval | Notion is an additional adapter over the same pipeline; retrieval quality does not depend on it. |

## Non-blocking evaluations

These spikes are optional engineering experiments. None blocks a block unless the block's own entry decisions say so.

| Issue | Position | Trigger / constraint |
| --- | --- | --- |
| #18 Campaign repository boundary | Adopted 2026-11-11 (ADR-008) | Now a sequenced step before 04A, not a spike. |
| #22 FluentValidation | Adopted for request shape only, 2026-11-11 (ADR-011) | Domain invariants stay in value objects; no pipeline behavior. |
| #21 Chat streaming transport | SSE adopted 2026-11-11 (ADR-010) | gRPC not evaluated; reopen only if a non-browser client needs typed streams. |
| Eval inspection tooling | Decided in 07 | Candidates: Genkit Developer UI and Langfuse (`05`). OpenTelemetry in the Aspire dashboard covers runtime traces from 04A. |
| #19 Vite+ / Oxlint / Oxfmt | Rejected 2026-11-03 (spike evidence in #19) | Keep Angular CLI, ESLint and Prettier. Reopen if Vite+ documents Angular support or Oxlint lints Angular templates. |
| #20 Aspire AppHost | Adopted for local development 2026-11-03 (ADR-007) | Spike evidence in #20. Not a deployment architecture. YARP and the architecture review were not evaluated; open them only on a concrete need. |
| #17 Codex Lithographica craft pass | After DS slices; maintainer-led | Does not block any block. |

## Open questions

- None on the current critical path. The timestamp question raised in #34 was resolved on 2026-11-11: the examples in `06` now match the `+00:00` offset the API emits.
- Entry decisions listed in the sequence table remain unresolved until recorded in their owning documents.

## AI engineering extension rule

Lorekeeper may use a small Python workbench (`uv`, Pydantic, pytest) and model-ecosystem libraries such as Hugging Face, SentenceTransformers and PyTorch for offline evaluation, reranking or inference experiments. These are not baseline product dependencies.

Adopt them incrementally:

1. start from the committed retrieval/eval corpus;
2. use Python only where the ecosystem materially improves experimentation or analysis;
3. compare candidate model behaviour against the simpler .NET/provider baseline;
4. promote a local model/runtime dependency only when measured quality gains justify latency, resource and delivery costs;
5. require an ADR before introducing a separately deployed Python/model service.

OpenTelemetry remains the default system-wide observability stack. Langfuse, Genkit Developer UI or another AI-specific platform may be added when prompt/model/retrieval inspection and eval workflows justify a second telemetry system, with explicit privacy and retention rules.

## Readiness records

- **Definition — Done.** First slice is campaign creation/selection plus empty Chat and Sources shells (`PRODUCT`, `00`, `02`); Campaign fields and invariants in `03`; endpoints, DTOs and errors in `06`. No blocker remains.
- **00 — Done.** pnpm, Node 24.15+, .NET 10, Angular 22, Tailwind, GitHub Actions, Docker Compose PostgreSQL 17, Playwright in `apps/web/e2e` and Conventional Commits are recorded in `04`, `08` and `09`. Evidence: frontend format/lint/test/build/E2E, backend restore/format/build/test and commitlint passed locally.
- **01 — Done.** US-01 in `02`, Campaign invariants in `03` and contracts in `06` define the slice. Evidence: UUID/name/timestamp invariants, PostgreSQL persistence with case-insensitive uniqueness, three campaign endpoints with RFC 7807 codes, Angular campaign selection plus URL-scoped Chat/Sources shells and Playwright creation/navigation flows; full frontend and backend gates passed. The original Block 01 design recorded English UI copy; ADR-005 supersedes it. The retired OpenSpec change for this block remains in git history up to commit `97099b6`.
- **DS — Done.** Decisions approved in #8 (fonts, language and delivery on 2026-10-01; Hallmark audit outcomes on 2026-10-30) and recorded in `DESIGN.md` and ADR-005. Slices 1–3 merged together in #29 on 2026-10-30. Evidence: five self-hosted font faces load from local `/media/*.woff2`; Tailwind v4 `@theme` tokens generate utilities; register and workspace use shared primitives and route states; Spanish copy with `lang="es"`, per-route titles and API error codes mapped to Spanish; accessibility review in #29 (verified contrast pairs, visible keyboard focus, field-associated errors, enlarged tap areas, reduced motion, no overflow at 390 px); format, lint, 27 unit tests, build and 2 E2E flows against PostgreSQL passed.
- **02 — Done (#31, closed 2026-11-04).** Entry decisions recorded on 2026-10-31: document generation, runtime exposure, generator, output locations and contract-diff policy in `06`; frontend query/command data access in ADR-006 and `docs/conventions/frontend.md`. Delivered in #34. Evidence: build-time OpenAPI document with generated hey-api types and Zod schemas committed; `contract:check` exits 1 after a `CampaignDto` change and the `Contract` CI job passed on #34; `/openapi/v1.json` and Swagger UI return 200 in Development and 404 in Production and Staging (integration tests); campaign feature uses `httpResource` queries and `CampaignCommands`; 48 frontend tests, 66 backend tests, format, lint, Release build, 2 Playwright flows against PostgreSQL and `pnpm audit --prod --audit-level=high` passed. Not verified in #34: `pnpm run dev` with these changes and the Swagger UI in a browser beyond its 200 response. The initial bundle grew from 316 to 418 kB raw (84 to 110 kB transfer), probably from Zod; no breakdown was recorded.
- **#18 — Ready.** ADR-008 defines the pattern; #18 lists the acceptance criteria; existing Campaign tests define unchanged behaviour. No open decision.
- **04A — Not started, ready.** Recorded on 2026-11-11: acceptance criteria (US-02 in `02`), Source invariants and chunking (`03`), Sources contract, limits and SSE events (`06`), private-data policy (`07`), stages and review flow (`12`), persistence (ADR-008), job queue (ADR-009), SSE (ADR-010), validation (ADR-011) and OpenTelemetry (`04`). Definition of Ready check:
  - Outcome, scope and dependencies: explicit; depends on #18. The inspect action moved to 05A.
  - Product and domain decisions: resolved, including update refused while a version is processing and purge in the removal transaction.
  - Acceptance criteria: US-02 covers success, limits, partial failure, update, removal and isolation.
  - Contracts and invariants: specified; DTO names, error codes and event names approved by the maintainer.
  - Verification and fixtures: listed in US-02; synthetic fixtures only.
  - Unresolved items on the critical path: none.
- **03 — Not started.** Entry decisions recorded in `05` on 2026-11-11; models are chosen during the block's own compatibility check. Sequenced after 05A.

## Cross-cutting gates

- Any Internet exposure, including an early preview, uses the Cloudflare Access boundary selected by ADR-002 and requires verified control of both UI and API plus prevention of direct-origin bypass. Selection alone does not unblock exposure.
- Campaign boundaries are enforced from the first persistence slice onward.
- Preparation, actual events and player knowledge must be modelled before ingestion; a DM note is not proof of player discovery.
- Evaluation starts before retrieval implementation: the corpus and expected sources are committed in 04A/05A; block 07 formalises gates.
- AI/model tooling does not enter the production runtime solely for portfolio value; the committed evaluation corpus must justify the added dependency.
- Each PR identifies its block, specification and acceptance evidence. No feature may rely on a placeholder as an approved decision.

## Workspace sequencing

[Workspace and ingestion UX](12-workspace-and-ingestion-ux.md) assigns campaign navigation to 01, Sources with file/text import to 04A, source search to 05A, Notion to 04B and Chat/citations to 06. 04A shows only En cola, Procesando and Disponible; 05A adds an indexing stage only if its lexical index is built separately, and 05B adds embedding. 04A alone must not claim retrieval readiness.

Codex Lithographica is adopted in [DESIGN.md](../DESIGN.md). Detailed component specifications and verified accessible token combinations are delivered by DS for current screens and by each UI block for its own screens.
