# Tasks — Block 01 Campaign Workspace Slice

## Review Workload Forecast

| Field | Value |
|-------|-------|
| Estimated changed lines | 900–1,400 |
| 400-line budget risk | High |
| Chained PRs recommended | Yes |
| Suggested split | PR 1 backend domain/API/persistence → PR 2 frontend routes/UI adapter → PR 3 E2E/docs/status verification |
| Delivery strategy | auto-chain, selected after ask-on-risk pause |
| Chain strategy | feature-branch-chain |
| Chain tooling | Use GitHub's own ecosystem where possible: `git` branches plus `gh pr create --base <branch>` / `gh pr edit --base <branch>` for branch-targeted chained PRs. |

Decision needed before apply: Resolved — use chained PRs.
Chained PRs recommended: Yes
Chain strategy: feature-branch-chain
400-line budget risk: High

## Notes for implementation

Strict TDD is active. Preserve RED → GREEN → TRIANGULATE → REFACTOR evidence for each work unit, keep Block 01 scope narrow, and do not add ingestion, AI chat, retrieval, citations, deletion, accounts, teams, authentication, or Internet-exposure access control.

Visible application UI copy for Block 01 is English. Tests that assert user-visible labels, states, errors, empty states, not-found states, and Chat/Sources shell copy must assert English strings.

Delivery uses a feature-branch chain:

```text
main
└─ block-01-tracker          # draft/no-merge tracker PR to main
   └─ block-01-backend       # PR 1 to tracker
      └─ block-01-frontend   # PR 2 to backend
         └─ block-01-e2e-docs # PR 3 to frontend
```

When PRs are created, prefer GitHub's native tooling through `gh` rather than custom metadata: set each PR base explicitly with `gh pr create --base <target-branch>` and adjust with `gh pr edit --base <target-branch>` if retargeting is needed. Keep each chained PR body explicit about start/end boundaries, dependency diagram, verification, rollback scope, and out-of-scope work.

## Work Unit 1 — Backend campaign domain and identifier validation

Start: no campaign domain model exists. Finish: domain invariants and identifier parsing are covered by unit tests without persistence or HTTP dependencies. Verification: `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~Campaign`. Rollback: remove the new campaign domain files and tests.

- [x] RED: add failing xUnit tests in `services/api/tests/Unit/Modules/Campaigns/Domain/CampaignNameTests.cs`, `CampaignTests.cs`, and `CampaignIdParserTests.cs` for trimming, invalid names, generated IDs/timestamps, canonical UUID acceptance, and malformed UUID rejection. <!-- sdd-owner: implementation -->
- [x] GREEN: implement `services/api/src/Api/Modules/Campaigns/Domain/CampaignName.cs`, `Campaign.cs`, and `CampaignIdParser.cs` with `campaign_name_invalid` and `campaign_id_invalid` result codes. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: add boundary tests for 1-character names, 120-character names, 121-character names, mixed-case UUID input, and lowercase canonical response formatting. <!-- sdd-owner: implementation -->
- [x] REFACTOR: keep domain result types local to `services/api/src/Api/Modules/Campaigns/Domain/` and avoid repositories, MediatR, base handlers, or source/chat placeholders. <!-- sdd-owner: implementation -->

## Work Unit 2 — Backend PostgreSQL persistence and duplicate protection

Start: domain unit tests pass. Finish: EF Core maps the `campaigns` table with PostgreSQL-enforced case-insensitive uniqueness and integration tests prove persistence boundaries. Verification: `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignsPersistence`. Rollback: remove EF package references, DbContext, migration, and persistence tests.

- [x] RED: add failing integration tests in `services/api/tests/Integration/Modules/Campaigns/CampaignsPersistenceTests.cs` using WebApplicationFactory/Testcontainers PostgreSQL for empty list, trimmed insert, database check constraints, duplicate casing conflict, and concurrent duplicate protection. <!-- sdd-owner: implementation -->
- [x] GREEN: add EF/Npgsql package references in `services/api/src/Api/Api.csproj` and Testcontainers/Npgsql test references in `services/api/tests/Integration/Integration.csproj`. <!-- sdd-owner: implementation -->
- [x] GREEN: implement `services/api/src/Api/Modules/Campaigns/Infrastructure/CampaignsDbContext.cs` and `CampaignEntityTypeConfiguration.cs` with snake_case columns, `uuid`, `varchar(120)`, `timestamptz`, generated `name_normalized`, check constraints, and unique index. <!-- sdd-owner: implementation -->
- [x] GREEN: add the EF migration under `services/api/src/Api/Modules/Campaigns/Infrastructure/Migrations/` creating only the Block 01 `campaigns` table and indexes. <!-- sdd-owner: implementation -->
- [x] GREEN: register `CampaignsDbContext` and PostgreSQL configuration in `services/api/src/Api/Program.cs`, `appsettings.json`, and `appsettings.Development.json` without adding production secrets. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: add an integration assertion that database unique/check violations are mapped without leaking constraint names or provider exception messages. <!-- sdd-owner: implementation -->
- [x] REFACTOR: keep EF Core as the persistence boundary for campaign slices and remove any speculative abstractions introduced during GREEN. <!-- sdd-owner: implementation -->

## Work Unit 3 — Backend campaign HTTP API

Start: persistence integration tests pass. Finish: `/api/campaigns` endpoints return DTOs and RFC 7807 errors with stable `code` extensions. Verification: `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignEndpoints`. Rollback: remove endpoint mapping, application handlers, DTOs, and endpoint tests.

- [x] RED: add failing endpoint tests in `services/api/tests/Integration/Modules/Campaigns/CampaignEndpointsTests.cs` for `GET /api/campaigns`, `POST /api/campaigns`, `GET /api/campaigns/{campaignId}`, invalid name, duplicate name, malformed ID, unknown ID, `Location`, UTC ISO timestamps, and no implicit active campaign. <!-- sdd-owner: implementation -->
- [x] GREEN: implement application slices under `services/api/src/Api/Modules/Campaigns/Application/CreateCampaign/`, `ListCampaigns/`, and `GetCampaign/` using direct `CampaignsDbContext` dependencies. <!-- sdd-owner: implementation -->
- [x] GREEN: implement DTOs/problem mapping in `services/api/src/Api/Modules/Campaigns/CampaignEndpoints.cs` and map endpoints from `services/api/src/Api/Program.cs`. <!-- sdd-owner: implementation -->
- [x] GREEN: return RFC 7807 Problem Details with stable `code` values `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, and `campaign_not_found`, plus field-level `name` information for name failures. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: add tests proving delete, source, ingestion, chat, retrieval, citation, account, team, and implicit-active campaign endpoints are not introduced by this slice. <!-- sdd-owner: implementation -->
- [x] REFACTOR: ensure OpenAPI metadata names and response descriptions stay narrow to the three Block 01 campaign endpoints. <!-- sdd-owner: implementation -->

## Work Unit 4 — Frontend campaign API adapter and typed state

Start: backend contract shape is known. Finish: Angular has a narrow handwritten campaign adapter and route-safe state types covered by tests. Verification: `pnpm run test -- --run campaigns`. Rollback: remove campaign feature adapter/model files and tests.

- [x] RED: add failing Vitest tests under `apps/web/src/app/features/campaigns/api/campaigns-api.service.spec.ts` and `apps/web/src/app/features/campaigns/campaign-id.spec.ts` for list/create/get success, Problem Details code mapping, network/recoverable errors, and malformed UUID detection. <!-- sdd-owner: implementation -->
- [x] GREEN: implement `apps/web/src/app/features/campaigns/models/campaign.model.ts`, `state/problem-details.ts`, `campaign-id.ts`, and `api/campaigns-api.service.ts` with only `listCampaigns`, `createCampaign`, and `getCampaign`. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: add adapter tests for `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, `campaign_not_found`, and unexpected error payloads. <!-- sdd-owner: implementation -->
- [x] REFACTOR: keep the handwritten adapter colocated under `apps/web/src/app/features/campaigns/` and document in code/tests that generated-client automation remains out of Block 01. <!-- sdd-owner: implementation -->

## Work Unit 5 — Frontend routes and campaign selection page

Start: adapter tests pass. Finish: `/campaigns` supports loading, empty, list, creation, validation, conflict, recoverable error, and explicit navigation states. Verification: `pnpm run test -- --run campaigns` and `pnpm run lint`. Rollback: remove campaign route entries, page component, styles, and tests.

- [x] RED: add failing route/component tests in `apps/web/src/app/features/campaigns/campaign-selection.page.spec.ts` for loading, empty, list success, creation loading, creation success, field-level invalid/conflict errors, recoverable retry, and explicit Chat/Sources links. <!-- sdd-owner: implementation -->
- [x] GREEN: register `/campaigns` in `apps/web/src/app/app.routes.ts` and implement `apps/web/src/app/features/campaigns/campaign-selection.page.ts` using Angular standalone APIs/signals. <!-- sdd-owner: implementation -->
- [x] GREEN: update shared shell entry points in `apps/web/src/app/app.component.html`, `app.component.ts`, and `app.component.css` only as needed to host routed campaign pages and preserve existing scaffold behavior. <!-- sdd-owner: implementation -->
- [x] GREEN: style the selection UI in `apps/web/src/styles.css` or component styles with Codex Lithographica tokens from `DESIGN.md`, English visible copy, semantic headings, visible focus, and responsive normal-flow links. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: add tests proving `/campaigns` never silently enters Chat or Sources and no stored active-campaign preference overrides URL selection. <!-- sdd-owner: implementation -->
- [x] REFACTOR: remove any disabled-looking future actions, placeholder ingestion controls, or hidden defaults introduced while building the selection page. <!-- sdd-owner: implementation -->

## Work Unit 6 — Frontend Chat and Sources workspace shells

Start: `/campaigns` route works. Finish: `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources` resolve campaign context from the URL and render unavailable-capability shells. Verification: `pnpm run test -- --run campaigns` and `pnpm run build`. Rollback: remove workspace route entries, shell components, and tests.

- [x] RED: add failing route/component tests in `apps/web/src/app/features/campaigns/chat-shell.page.spec.ts`, `sources-shell.page.spec.ts`, and `campaign-workspace-shell.component.spec.ts` for route loading, valid campaign context, malformed ID state, unknown ID state, and no previous-campaign fallback. <!-- sdd-owner: implementation -->
- [x] GREEN: add `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources` to `apps/web/src/app/app.routes.ts` and implement `chat-shell.page.ts`, `sources-shell.page.ts`, and `campaign-workspace-shell.component.ts`. <!-- sdd-owner: implementation -->
- [x] GREEN: render English Chat shell copy stating AI chat, source-backed querying, retrieval, citations, conversation history, streaming, and draft persistence are unavailable in this slice. <!-- sdd-owner: implementation -->
- [x] GREEN: render English Sources shell copy stating ingestion, upload, paste, Notion import, update, removal, indexing, retry, progress, and source lifecycle actions are unavailable in this slice. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: add negative assertions that no query form, submit button, upload/paste/import controls, citation affordances, source-management actions, retry/progress widgets, or disabled future controls are present. <!-- sdd-owner: implementation -->
- [x] REFACTOR: keep campaign route loading isolated so old route data cannot display while a new `campaignId` is resolving. <!-- sdd-owner: implementation -->

## Work Unit 7 — Focused E2E smoke flow

Start: backend and frontend route behavior pass lower-level tests. Finish: one Playwright smoke proves the first operator flow across the real app boundary. Verification: `pnpm run e2e`. Rollback: remove the new E2E spec and any test-only setup added for it.

- [x] RED: add a failing Playwright spec in `apps/web/e2e/campaign-workspace.spec.ts` that opens `/campaigns`, creates `Ash Crown`, sees it listed, navigates to Chat, and navigates to Sources. <!-- sdd-owner: implementation -->
- [x] GREEN: add only necessary E2E setup/configuration in `apps/web/e2e/playwright.config.ts` or existing test bootstrap to point at the local API/database without adding mock-only product behavior. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: assert English unavailable Chat and Sources copy in the smoke test and verify no ingestion or AI controls appear. <!-- sdd-owner: implementation -->
- [x] REFACTOR: keep the E2E flow focused on Block 01 and move detailed validation/error coverage back to component or integration tests. <!-- sdd-owner: implementation -->

## Work Unit 8 — Documentation, status, and full verification

Start: implementation behavior is complete in tests. Finish: status and documentation reflect the implemented Block 01 slice and all applicable checks are recorded. Verification: full apply/verify command set from `openspec/config.yaml`. Rollback: revert documentation/status updates separately from code if they misrepresent behavior.

- [x] RED: identify any implementation-discovered spec gap in `openspec/changes/block-01-campaign-workspace-slice/proposal.md`, `specs/campaigns/spec.md`, `specs/workspace/spec.md`, or `design.md` before changing behavior. <!-- sdd-owner: implementation -->
- [x] GREEN: update `docs/10-roadmap.md` Block 01 readiness/exit evidence and any affected command/status notes only after behavior and tests exist. <!-- sdd-owner: implementation -->
- [x] GREEN: update `docs/06-api-contracts.md`, `docs/03-domain-model.md`, or `docs/12-workspace-and-ingestion-ux.md` only if implementation reveals a documented Block 01 gap, keeping PRODUCT.md behavior unchanged unless the spec is explicitly changed first. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: run `pnpm run test`, `pnpm run build`, `dotnet build services/api/Lorekeeper.slnx --configuration Release`, and `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` as the apply gate and record any deviations. <!-- sdd-owner: implementation -->
- [x] TRIANGULATE: run `pnpm run lint`, `pnpm run test`, `pnpm run build`, `pnpm run e2e`, `dotnet format services/api/Lorekeeper.slnx --verify-no-changes`, `dotnet build services/api/Lorekeeper.slnx --no-restore --configuration Release`, and `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` as the verify gate. <!-- sdd-owner: implementation -->
- [x] REFACTOR: remove dead code, unused test helpers, unverified placeholders, generated artifacts not required for Block 01, and any accidental out-of-scope affordances before review. <!-- sdd-owner: implementation -->

### Work Unit 8 final evidence

- Documentation comparison identified and corrected implementation-backed gaps in the roadmap, campaign domain, API-contract, and workspace UX documents. No product behavior or next-roadmap feature was changed.
- Cleanup found no safe accidental Block 01 artifact to remove. `.codegraph/`, `bash.exe.stackdump`, Lazy Lands, external PostgreSQL, and non-Block-01 OpenSpec directories were left untouched.
- The exact apply and verify command sets passed. The final retry started Docker Desktop locally, waited for its Linux engine, started only `lorekeeper-postgres` with its existing named volume, confirmed migrations were current, and passed Playwright E2E plus all 28 backend tests.
- All Work Unit 8 checkboxes are complete. Docker Desktop is stopped after the final gates.
