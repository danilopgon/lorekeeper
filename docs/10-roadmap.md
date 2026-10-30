# 10 — Implementation Roadmap

## Status and execution rule

`Not started` · `In progress` · `Blocked` · `Done`.

Apply the blocking Definition of Ready in `08-quality-strategy.md` before every block. Dependencies are necessary but not sufficient: resolve the entry decisions below in their owning documents. Record status and readiness evidence here; the work plan lives in the block's GitHub issue and verification evidence in each PR (see `conventions/documentation.md`). Unknowns on another block's path do not block independent ready work.

## Current focus

- **Done:** Definition, 00, 01.
- **In progress:** DS — frontend design-system foundation (#8). It gates new UI work, starting with 04A Sources screens.
- **Ready to start in parallel:** 02 — generated OpenAPI client, once its entry decisions are recorded in `06`. It touches no UI and does not wait for DS.
- **First useful version target:** 05A — the operator can import Markdown/text notes into a campaign and search them with source-backed fragments, without an LLM.

## Sequence

Block IDs are kept for traceability with issues and history; the order column is the execution order. Revised 2026-10-29 (see [revision notes](#revision-2026-10-29)).

| Order | Block | Outcome | Depends on | Required decisions before implementation | Exit evidence | Status |
| ---: | --- | --- | --- | --- | --- | --- |
| — | Definition | Product and scope | none | Personal multicampaign boundaries, first slice, non-goals and observable acceptance in PRODUCT, 00–03 | First slice passes Definition of Ready | Done |
| — | 00 | Skeleton and CI | Definition | Runtime/package versions, layout, E2E location, commands and CI tools in 04/08/09 | Scaffold builds; checks run; AGENTS commands updated | Done |
| — | 01 | Angular → .NET → PostgreSQL slice | 00 | Campaign schema/invariants, URL campaign selection, DTOs/errors, minimal UI states in 02/03/06/DESIGN | First flow and database isolation tests pass | Done |
| 1 | DS | Frontend design-system foundation (#8) | 01 | Approved in #8 on 2026-10-01: self-hosted fonts, Spanish UI copy (ADR-005 lands with that slice), delivery slices | Fonts load locally; tokens generate Tailwind v4 utilities; current screens use shared primitives and states; Spanish UI with `lang="es"`; accessibility review recorded | In progress |
| 1 | 02 | Generated OpenAPI client | 01 | Build-time OpenAPI document generation (none exists yet), generator, output location/versioning and contract-diff policy in 06 | Client generation and CI contract-diff gate run; Campaign adapter consumes the generated client | Not started |
| 2 | 04A | Sources: Markdown/text and pasted text | 01, 02, DS | Upload/paste limits, canonical document schema, knowledge status, structural chunking, job lifecycle/progress/retry, identity/version publication, removal and private-data policy in 02/03/05/06/07/12; persistence pattern for Sources (#18) and command validation approach (#22) | Supported inputs imported per campaign; partial failure, update, removal and campaign isolation verified against PostgreSQL; initial eval fixtures committed | Not started |
| 3 | 05A | Lexical source search (FTS) | 04A | FTS language/configuration, ranking and snippet/locator contract, search placement in the workspace (12/DESIGN), initial eval corpus with expected sources | Campaign-scoped search returns fragments with real locators; lexical baseline (Recall@K/MRR) recorded; no cross-campaign leakage | Not started |
| 3 | 03 | AI ports and deterministic fake | 01 | Adapter/model settings, dimensions, limits and failure policy in 05/09 | Fake verifies orchestration; configured adapter compatibility checked before live use | Not started |
| 4 | 05B | Vector retrieval | 03, 05A | Embedding model/dimensions, vector index configuration, candidate limits; whether offline Python comparisons add value | Vector baseline recorded beside the lexical one on the same corpus; no campaign leakage | Not started |
| 5 | 06 | Fusion and grounded answer (Chat) | 05B; #21 resolved | RRF settings, reranker decision, budgets, citations, contradictions, insufficient evidence, streaming transport and generation criteria in 02/05/06; conversation persistence and drafts in 12 | Answers trace to campaign evidence; comparison against baselines recorded | Not started |
| 6 | 04B | Notion ingestion | 04A | Root/credentials, traversal, supported blocks, sync and remote deletion in 03/05/06/07/12 | Notion uses the same canonical pipeline and source lifecycle; campaign isolation verified | Not started |
| 7 | 07 | Regression gates | 06 | Measured thresholds/tolerances, frozen versus live eval execution in 08; whether Langfuse or equivalent adds value beyond OpenTelemetry | Repeatable gates with documented baseline and cost/variance policy | Not started |
| 8 | 08 | Complete flow and deployment | 07 | Hosting, Cloudflare Access/Tunnel details, backups, limits and rollback in 07/09 and ADR-002 | Critical flow passes; any remote exposure passes Access/origin-bypass smoke verification | Not started |

Rows sharing an order number may run in parallel. 04B may be scheduled any time after 04A; it is placed after 06 because Markdown exports already cover Notion content for the first useful version, and it should reuse a pipeline proven by 05A.

ADR-003 records MediatR as the narrow Campaign Application dispatch boundary. ADR-004 refines its HTTP adapter choice. Neither marks a future block ready or introduces pipeline behaviors, repositories or generic controller abstractions.

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
| #18 Campaign repository boundary | Decided during 04A design | Sources needs atomic version publication, a real transactional boundary. Choose the persistence pattern there; refactor Campaigns only if the same pattern is adopted. No preventive refactor. |
| #22 FluentValidation | Decided during 04A design | 04A introduces the next commands. Until then current domain-result validation is retained. |
| #21 Chat streaming transport | Before 06 Definition of Ready | 03 may define a transport-agnostic async stream port. |
| #19 Vite+ / Oxlint / Oxfmt | After 05A, or earlier only if frontend tooling becomes a measured pain | Must not change build/lint/test scripts while DS is in progress. Settle before #20 if it changes scripts. |
| #20 Aspire AppHost | After 05A | More value once the system has more resources (AI provider, background jobs). Not a prerequisite for 03. |
| #17 Codex Lithographica craft pass | After DS slices; maintainer-led | Does not block any block. |

## Open questions

- None on the current critical path. Entry decisions listed in the sequence table remain unresolved until recorded in their owning documents.

## AI engineering extension rule

Lorekeeper may use a small Python workbench (`uv`, Pydantic, pytest) and model-ecosystem libraries such as Hugging Face, SentenceTransformers and PyTorch for offline evaluation, reranking or inference experiments. These are not baseline product dependencies.

Adopt them incrementally:

1. start from the committed retrieval/eval corpus;
2. use Python only where the ecosystem materially improves experimentation or analysis;
3. compare candidate model behaviour against the simpler .NET/provider baseline;
4. promote a local model/runtime dependency only when measured quality gains justify latency, resource and delivery costs;
5. require an ADR before introducing a separately deployed Python/model service.

OpenTelemetry remains the default system-wide observability stack. Langfuse or another AI-specific platform may be added when prompt/model/retrieval inspection and eval workflows justify a second telemetry system, with explicit privacy and retention rules.

## Readiness records

- **Definition — Done.** First slice is campaign creation/selection plus empty Chat and Sources shells (`PRODUCT`, `00`, `02`); Campaign fields and invariants in `03`; endpoints, DTOs and errors in `06`. No blocker remains.
- **00 — Done.** pnpm, Node 24.15+, .NET 10, Angular 22, Tailwind, GitHub Actions, Docker Compose PostgreSQL 17, Playwright in `apps/web/e2e` and Conventional Commits are recorded in `04`, `08` and `09`. Evidence: frontend format/lint/test/build/E2E, backend restore/format/build/test and commitlint passed locally.
- **01 — Done.** US-01 in `02`, Campaign invariants in `03` and contracts in `06` define the slice. Evidence: UUID/name/timestamp invariants, PostgreSQL persistence with case-insensitive uniqueness, three campaign endpoints with RFC 7807 codes, Angular campaign selection plus URL-scoped Chat/Sources shells and Playwright creation/navigation flows; full frontend and backend gates passed. The original Block 01 design recorded English UI copy; the Spanish UI decision approved in #8 supersedes it once its slice lands. The retired OpenSpec change for this block remains in git history up to commit `97099b6`.
- **DS — In progress.** Decisions approved in #8 on 2026-10-01 (fonts, language, delivery). Slice 1 (fonts and Tailwind v4 tokens) is implemented on a local branch pending this replanning. Remaining blocker: none.
- **02 — Not started.** Required before starting: OpenAPI document generation approach, generator, generated output location/versioning and contract-diff policy recorded in `06`.

## Cross-cutting gates

- Any Internet exposure, including an early preview, uses the Cloudflare Access boundary selected by ADR-002 and requires verified control of both UI and API plus prevention of direct-origin bypass. Selection alone does not unblock exposure.
- Campaign boundaries are enforced from the first persistence slice onward.
- Preparation, actual events and player knowledge must be modelled before ingestion; a DM note is not proof of player discovery.
- Evaluation starts before retrieval implementation: the corpus and expected sources are committed in 04A/05A; block 07 formalises gates.
- AI/model tooling does not enter the production runtime solely for portfolio value; the committed evaluation corpus must justify the added dependency.
- Each PR identifies its block, specification and acceptance evidence. No feature may rely on a placeholder as an approved decision.

## Workspace sequencing

[Workspace and ingestion UX](12-workspace-and-ingestion-ux.md) assigns campaign navigation to 01, Sources with file/text import to 04A, source search to 05A, Notion to 04B and Chat/citations to 06. In 04A the "Indexing" stage covers the lexical index delivered with 05A; vector indexing arrives with 05B, so 04A alone must not claim retrieval readiness.

Codex Lithographica is adopted in [DESIGN.md](../DESIGN.md). Detailed component specifications and verified accessible token combinations are delivered by DS for current screens and by each UI block for its own screens.
