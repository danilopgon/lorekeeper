# 10 — Implementation Roadmap

## Status and execution rule

`Not started` · `In progress` · `Blocked` · `Done`.

Apply the blocking Definition of Ready in `08-quality-strategy.md` before every block. Dependencies are necessary but not sufficient: resolve the entry decisions below in their owning documents. Record evidence and status here before coding. Unknowns on another block's path do not block independent ready work.

## Current block

**Block:** 01 — Angular → .NET → PostgreSQL slice
**Status:** Done
**Outcome:** campaign creation/selection plus empty campaign workspace shells, delivered through chained review slices.

Definition remains Done: personal single-operator use, multiple isolated campaigns, no multiuser scaffolding, access control required before Internet exposure. First slice: campaign creation/selection plus empty campaign workspace shell with Chat and Sources routes, without ingestion or AI. Minimal campaign fields are `id`, `name`, `createdAt` and `updatedAt`; `name` is required, trimmed, 1–120 characters and unique case-insensitively. Active campaign is represented in the URL: `/campaigns`, `/campaigns/{campaignId}/chat`, `/campaigns/{campaignId}/sources`. Campaign deletion is excluded from the first slice.

Block 00 readiness decisions are recorded in `04`, `08` and `09`: pnpm, Node 24.15+, .NET 10, Angular 22, Tailwind aligned to `DESIGN.md`, GitHub Actions, Docker Compose PostgreSQL 17, Playwright E2E in `apps/web/e2e`, and Conventional Commits enforced through CI plus local hooks. Python/AI evaluation workbench remains planned for later and is not an active block 00 dependency or check. ADR-002 now selects Cloudflare Access for the future personal remote-access boundary; implementation and verification remain deferred to the deployment gate.

Block 00 exit evidence: pnpm workspace, Angular 22/Tailwind frontend, Playwright in `apps/web/e2e`, .NET 10 API/test solution, Docker Compose PostgreSQL 17, GitHub Actions and Conventional Commit hooks are scaffolded. Local checks have passed for frontend format/lint/test/build/E2E, backend restore/format/build/test and commitlint.

Block 01 entry decisions are confirmed against `02`, `03`, `06`, `DESIGN.md` and the SDD change `openspec/changes/block-01-campaign-workspace-slice`. Implementation completed through chained review slices.

## Blocks and gates

| Block | Outcome / dependency | Required decisions before implementation | Exit evidence |
| --- | --- | --- | --- |
| Definition | Product and scope / none | Personal multicampaign boundaries, first slice, non-goals and observable acceptance in PRODUCT, 00–03 | First slice passes Definition of Ready; unresolved later decisions explicitly assigned |
| 00 | Skeleton and CI / Definition | Runtime/package versions, package manager, module/project layout, one E2E location, real commands and CI tools in 04/08/09 | Scaffold builds; applicable checks run; AGENTS commands updated |
| 01 | Angular → .NET → PostgreSQL slice / 00 | Campaign schema/invariants, explicit campaign selection, endpoint DTOs/errors, minimal UI states/tokens in 02/03/06/DESIGN | Accepted first flow and database isolation tests pass |
| 02 | Generated OpenAPI client / 01 | Generator, output location/versioning and contract-diff policy in 06 | Client generation and contract gate run |
| 03 | AI ports and deterministic fake / 01 | NaN adapter/model settings, dimensions, limits, failure policy in 05/09 | Fake verifies orchestration; configured adapter compatibility checked before live use |
| 04A | Sources: Markdown/text and pasted text / 03 | Upload/review limits, canonical schema, knowledge status, chunking, progress/retry, identity/version publication, removal and private-data policy in 02/03/05/06/07/12 | Supported inputs available per campaign; partial failure/update/isolation verified; initial eval fixtures ready |
| 04B | Notion ingestion / 04A | Root/credentials, traversal, supported blocks, sync and remote deletion in 03/05/06/07/12 | Notion uses the same canonical pipeline and source lifecycle; campaign isolation verified |
| 05 | FTS + vector retrieval / 04B | FTS language, vector/index configuration and candidate limits; initial eval dataset exists; decide whether offline Python experiments add value for embedding/retrieval comparisons | Separate lexical/vector baselines recorded; no campaign leakage; any experimental comparison is reproducible from committed inputs |
| 06 | Fusion and grounded answer / 05 | RRF settings, reranker decision, budgets, citations, contradictions, insufficient evidence, transport and generation criteria in 02/05/06; compare no-reranker/provider/local candidates before promoting runtime complexity | Answers trace to campaign evidence; comparison against baselines recorded; any promoted reranker has measured quality/latency/resource evidence |
| 07 | Regression gates / 06 | Measured thresholds/tolerances, frozen versus live eval execution in 08; decide whether Langfuse or equivalent adds material value beyond OpenTelemetry | Repeatable gates with documented baseline and cost/variance policy; AI observability choice and privacy policy recorded |
| 08 | Complete flow and deployment / 07 | Hosting, Cloudflare Access/Tunnel deployment details, backups, limits and rollback in 07/09 and ADR-002; any local-model runtime must have artefact/version/fallback/runbook decisions | Critical flow passes; any remote exposure passes Access/origin-bypass smoke verification; promoted AI runtime dependencies are operationally reproducible |

Block 00 is **Done**. Block 01 is **Done**. Blocks 02–08 are **Not started**. Mark a block **Blocked** when readiness assessment identifies an unresolved required decision; document the exact condition below.

ADR-003 records the narrow post-Block-01 Campaign endpoint architecture improvement: create/list/get retain their Minimal API routes, OpenAPI metadata and Problem Details codes while dispatching explicit application messages through MediatR. It does not mark a future block ready or introduce pipeline behaviors, repositories, MVC, or shared abstractions.

## AI engineering extension rule

Lorekeeper may use a small Python workbench (`uv`, Pydantic, pytest) and model-ecosystem libraries such as Hugging Face, SentenceTransformers and PyTorch for offline evaluation, reranking or inference experiments. These are not baseline product dependencies.

Adopt them incrementally:

1. start from the committed retrieval/eval corpus;
2. use Python only where the ecosystem materially improves experimentation or analysis;
3. compare candidate model behaviour against the simpler .NET/provider baseline;
4. promote a local model/runtime dependency only when measured quality gains justify latency, resource and delivery costs;
5. require an ADR before introducing a separately deployed Python/model service.

OpenTelemetry remains the default system-wide observability stack. Langfuse or another AI-specific platform may be added when prompt/model/retrieval inspection and eval workflows justify a second telemetry system, with explicit privacy and retention rules.

## Readiness record (required per block)

- Block and status: Definition — Done.
- Entry decisions and links to resolved specifications: first implementation slice is campaign creation/selection plus empty Chat and Sources workspace shell (`PRODUCT`, `00`, `02`); Campaign fields and invariants are defined (`03`); first-slice campaign endpoints, DTOs and error mapping are defined (`06`); delete campaign, ingestion and AI are excluded from the slice (`PRODUCT`, `00`, `02`, `03`, `06`).
- Remaining blocker, target document and unblock condition: no Definition blocker remains.
- Acceptance criteria and verification plan: US-01 in `02` defines creation, validation, URL campaign selection, Chat/Sources shells and excluded behaviours; verification covers domain/unit validation, backend integration, frontend route/component states and one E2E smoke test.
- Exit evidence and remaining limitations: Documentation-only definition update; no scaffold, code, tests, generated client or implementation evidence exists yet.

- Block and status: 00 — Done.
- Entry decisions and links to resolved specifications: pnpm, Node 24.15+, .NET 10, Tailwind, GitHub Actions, Docker Compose PostgreSQL 17, initial Playwright E2E location `apps/web/e2e`, Conventional Commits enforcement, and inactive Python/AI workbench are recorded in `04`, `08` and `09`.
- Remaining blocker, target document and unblock condition: none for block 00. Internet exposure remains gated by `07`/`09`, but it does not block local work.
- Acceptance criteria and verification plan: scaffold creates real frontend/backend/local database/CI command surfaces; AGENTS commands are executable; applicable checks run locally.
- Exit evidence and remaining limitations: `pnpm install`; `pnpm run format:check`; `pnpm run lint`; `pnpm run test`; `pnpm run build`; `pnpm run e2e`; `dotnet restore services/api/Lorekeeper.slnx`; `dotnet format services/api/Lorekeeper.slnx --verify-no-changes`; `dotnet build services/api/Lorekeeper.slnx --no-restore --configuration Release`; `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release`; commitlint smoke check. CI is configured but not observed remotely in this local evidence.

- Block and status: 01 — Done.
- Entry decisions and links to resolved specifications: campaign schema, invariants and exclusions are defined in `02`, `03` and `06`; UI visual direction and English visible copy are defined in `DESIGN.md` and `openspec/changes/block-01-campaign-workspace-slice`; SDD proposal/spec/design/tasks are present for the Block 01 campaign workspace slice.
- Remaining blocker, target document and unblock condition: none for Block 01. Internet exposure remains gated by `07`/`09` and is out of scope.
- Acceptance criteria and verification plan: US-01 plus the OpenSpec `campaigns` and `workspace` specs define campaign creation/selection, validation, explicit URL campaign context, empty Chat/Sources shells, excluded behaviours and accessible states; verification covers backend unit/integration tests, frontend route/component tests, focused E2E and full quality gates.
- Exit evidence and remaining limitations: chained slices implement campaign UUID/name/timestamp invariants, PostgreSQL persistence and case-insensitive uniqueness, the three campaign API endpoints and RFC 7807 codes, Angular campaign selection plus URL-scoped Chat/Sources unavailable shells, and focused Playwright creation/navigation flows. Full final verification passed: frontend lint/test/build/E2E, backend format/build/unit/integration tests. Docker Desktop was used only for the final local PostgreSQL/Testcontainers gates and is stopped afterward. No ingestion, AI, retrieval, citations, deletion, auth, accounts, teams, or Internet exposure is included.

## Cross-cutting gates

- Any Internet exposure, including an early preview, uses the Cloudflare Access boundary selected by ADR-002 and requires verified control of both UI and API plus prevention of direct-origin bypass. Selection alone does not unblock exposure.
- Campaign boundaries are enforced from the first persistence slice onward.
- Preparation, actual events and player knowledge must be modelled before ingestion; a DM note is not proof of player discovery.
- Evaluation starts before retrieval implementation; block 07 formalises gates.
- AI/model tooling does not enter the production runtime solely for portfolio value; the committed evaluation corpus must justify the added dependency.
- Each PR identifies its block, specification and acceptance evidence. No feature may rely on a placeholder as an approved decision.

## First UX approach: sequencing clarification

[Workspace and ingestion UX](12-workspace-and-ingestion-ux.md) assigns campaign creation/selection and navigation to 01, Sources/file-text import to 04A, Notion to 04B, and Chat/citations to 06. The first implementation slice narrows block 01 to campaign creation/selection and empty workspace shells only; no ingestion or AI capability is included. Define the navigation/states before the corresponding UI work. Source publication/progress contracts are specified in 04A; vector indexing is completed with 05, so an intermediate parser/storage milestone must not claim production retrieval readiness. End-to-end source availability is verified once indexing exists. This decomposition preserves the existing gates and does not mark blocks ready or complete.

## Design decision resolved

Codex Lithographica is adopted in [DESIGN.md](../DESIGN.md), including conditional evidence blocks, real citation locators, responsive and accessibility requirements. The visual-direction decision is resolved; detailed screen/component specifications and verified accessible token combinations remain block 01 / affected-UI entry requirements. This documentation update does not mark implementation blocks complete.
