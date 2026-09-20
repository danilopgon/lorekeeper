# Apply Progress — Block 01 Campaign Workspace Slice

## Structured status consumed

- Native status schema: `gentle-ai.sdd-status` v2
- Change: `block-01-campaign-workspace-slice`
- Apply state: `ready`
- Next recommended on entry: `apply`
- Artifact store used by this executor: `openspec`
- Action context: repo-local workspace `C:\Users\TONBO\Dev\lorekeeper`; allowed edit root `C:\Users\TONBO\Dev\lorekeeper`
- Parent-acquired attempt token used for this phase: `sha256:557d42953935dedcde7102272b4407d746e5df18bb77e9916152fe9ade99cbf9`
- ActionContext warnings: none; work stayed inside the authoritative workspace and backend slice.

## Workload / PR boundary

- Delivery strategy: `auto-chain`
- Chain strategy: `feature-branch-chain`
- Intended first child PR: `block-01-backend` targeting tracker branch `block-01-tracker`
- Review budget for the acquired attempt: 400 changed lines.
- Completed slice: Work Unit 1 only — backend campaign domain and identifier validation.
- Revised PR boundary: stop before Work Units 2–3. PostgreSQL persistence and HTTP API would require EF packages, migrations, integration harness, endpoint handlers, and endpoint tests, which would not fit coherently with Work Unit 1 inside the 400-line attempt budget.

Dependency diagram:

```text
main
└─ block-01-tracker
   └─ block-01-backend 📍 (current: campaign domain and ID validation only)
      └─ block-01-persistence-api (recommended next backend child slice)
         └─ block-01-frontend
            └─ block-01-e2e-docs
```

## Completed tasks and persisted checkboxes

- [x] RED: add failing xUnit tests in `services/api/tests/Unit/Modules/Campaigns/Domain/CampaignNameTests.cs`, `CampaignTests.cs`, and `CampaignIdParserTests.cs` for trimming, invalid names, generated IDs/timestamps, canonical UUID acceptance, and malformed UUID rejection.
- [x] GREEN: implement `services/api/src/Api/Modules/Campaigns/Domain/CampaignName.cs`, `Campaign.cs`, and `CampaignIdParser.cs` with `campaign_name_invalid` and `campaign_id_invalid` result codes.
- [x] TRIANGULATE: add boundary tests for 1-character names, 120-character names, 121-character names, mixed-case UUID input, and lowercase canonical response formatting.
- [x] REFACTOR: keep domain result types local to `services/api/src/Api/Modules/Campaigns/Domain/` and avoid repositories, MediatR, base handlers, or source/chat placeholders.

The corresponding Work Unit 1 checkboxes were updated in `openspec/changes/block-01-campaign-workspace-slice/tasks.md`.

## TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 1 — Backend campaign domain and identifier validation | Added domain tests first; `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~Campaign` failed with missing `Api.Modules.Campaigns.Domain` types. | Added `CampaignName`, `Campaign`, `CampaignIdParser`, and local `DomainResult<T>`; focused campaign tests passed. | Boundary cases included 1-character names, 120-character names, 121-character names after trim, mixed-case UUID parsing, malformed compact UUID rejection, and lowercase UUID formatting. | Domain result type stayed local under `Modules/Campaigns/Domain`; no persistence, HTTP, MediatR, repository, source, chat, ingestion, auth, account, team, deletion, or AI placeholders were introduced. |

## Files changed

- `services/api/src/Api/Modules/Campaigns/Domain/Campaign.cs`
- `services/api/src/Api/Modules/Campaigns/Domain/CampaignIdParser.cs`
- `services/api/src/Api/Modules/Campaigns/Domain/CampaignName.cs`
- `services/api/src/Api/Modules/Campaigns/Domain/DomainResult.cs`
- `services/api/tests/Unit/Modules/Campaigns/Domain/CampaignIdParserTests.cs`
- `services/api/tests/Unit/Modules/Campaigns/Domain/CampaignNameTests.cs`
- `services/api/tests/Unit/Modules/Campaigns/Domain/CampaignTests.cs`
- `openspec/changes/block-01-campaign-workspace-slice/tasks.md`
- `openspec/changes/block-01-campaign-workspace-slice/apply-progress.md`

## Test commands run

1. `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~Campaign`
   - RED result: failed as expected because domain types did not exist.
2. `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~Campaign`
   - GREEN/TRIANGULATE result: passed; Unit suite reported 13 campaign tests passed. Integration suite had no matching campaign tests for this Work Unit.

## Deviations from design

- No design behavior was changed.
- Work Units 2–3 were intentionally deferred to preserve the acquired 400-line review boundary. This means EF Core persistence, PostgreSQL uniqueness, migrations, application handlers, and HTTP endpoints are not implemented yet.

## Remaining tasks

Exact unchecked implementation-owned task lines remaining:

- [ ] RED: add failing integration tests in `services/api/tests/Integration/Modules/Campaigns/CampaignsPersistenceTests.cs` using WebApplicationFactory/Testcontainers PostgreSQL for empty list, trimmed insert, database check constraints, duplicate casing conflict, and concurrent duplicate protection. <!-- sdd-owner: implementation -->
- [ ] GREEN: add EF/Npgsql package references in `services/api/src/Api/Api.csproj` and Testcontainers/Npgsql test references in `services/api/tests/Integration/Integration.csproj`. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement `services/api/src/Api/Modules/Campaigns/Infrastructure/CampaignsDbContext.cs` and `CampaignEntityTypeConfiguration.cs` with snake_case columns, `uuid`, `varchar(120)`, `timestamptz`, generated `name_normalized`, check constraints, and unique index. <!-- sdd-owner: implementation -->
- [ ] GREEN: add the EF migration under `services/api/src/Api/Modules/Campaigns/Infrastructure/Migrations/` creating only the Block 01 `campaigns` table and indexes. <!-- sdd-owner: implementation -->
- [ ] GREEN: register `CampaignsDbContext` and PostgreSQL configuration in `services/api/src/Api/Program.cs`, `appsettings.json`, and `appsettings.Development.json` without adding production secrets. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add an integration assertion that database unique/check violations are mapped without leaking constraint names or provider exception messages. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: keep EF Core as the persistence boundary for campaign slices and remove any speculative abstractions introduced during GREEN. <!-- sdd-owner: implementation -->
- [ ] RED: add failing endpoint tests in `services/api/tests/Integration/Modules/Campaigns/CampaignEndpointsTests.cs` for `GET /api/campaigns`, `POST /api/campaigns`, `GET /api/campaigns/{campaignId}`, invalid name, duplicate name, malformed ID, unknown ID, `Location`, UTC ISO timestamps, and no implicit active campaign. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement application slices under `services/api/src/Api/Modules/Campaigns/Application/CreateCampaign/`, `ListCampaigns/`, and `GetCampaign/` using direct `CampaignsDbContext` dependencies. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement DTOs/problem mapping in `services/api/src/Api/Modules/Campaigns/CampaignEndpoints.cs` and map endpoints from `services/api/src/Api/Program.cs`. <!-- sdd-owner: implementation -->
- [ ] GREEN: return RFC 7807 Problem Details with stable `code` values `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, and `campaign_not_found`, plus field-level `name` information for name failures. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add tests proving delete, source, ingestion, chat, retrieval, citation, account, team, and implicit-active campaign endpoints are not introduced by this slice. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: ensure OpenAPI metadata names and response descriptions stay narrow to the three Block 01 campaign endpoints. <!-- sdd-owner: implementation -->

Frontend Work Units 4–7 and documentation/full-verification Work Unit 8 remain intentionally untouched in this backend slice.

## Rollback boundary

Remove the seven new domain/test files listed above and revert the four Work Unit 1 checkbox updates in `tasks.md`. No migrations, packages, configuration, persistence, endpoints, frontend code, or runtime data were introduced.

---

## Apply update — backend persistence slice (Work Unit 2)

### Structured status consumed

- Native status schema: `gentle-ai.sdd-status` v2
- Change: `block-01-campaign-workspace-slice`
- Apply state on entry: `ready`
- Next recommended on entry: `apply`
- Artifact store used by this executor: `openspec`
- Action context: repo-local workspace `C:\Users\TONBO\Dev\lorekeeper`; allowed edit root `C:\Users\TONBO\Dev\lorekeeper`
- Parent-acquired attempt token used for this phase: `sha256:c5043560d08a671b78c1aa4f0baa4e782526fa5a8f49608353a6840bf3b748dd`
- ActionContext warnings: none; edits stayed inside the authoritative workspace and backend scope.

### Workload / PR boundary

- Delivery strategy: `auto-chain`
- Chain strategy: feature-branch chain; current branch `feat/block-01-persistence-api` targets `feat/block-01-domain`.
- Attempt changed-line budget: 500 lines.
- Completed slice in this update: Work Unit 2 — backend PostgreSQL persistence and duplicate protection.
- Revised PR boundary: stop before Work Unit 3. The persistence slice adds EF/Npgsql dependencies, DbContext mapping, migration files, configuration, and Testcontainers integration tests; adding endpoint handlers/tests in the same attempt would risk exceeding the 500-line budget and would be a separate cohesive HTTP API review unit.

Dependency diagram:

```text
main
└─ block-01-tracker
   └─ feat/block-01-domain
      └─ feat/block-01-persistence-api 📍 (current: PostgreSQL persistence only)
         └─ feat/block-01-campaign-api (recommended next backend child slice)
            └─ block-01-frontend
               └─ block-01-e2e-docs
```

### Completed tasks and persisted checkboxes

- [x] RED: add failing integration tests in `services/api/tests/Integration/Modules/Campaigns/CampaignsPersistenceTests.cs` using WebApplicationFactory/Testcontainers PostgreSQL for empty list, trimmed insert, database check constraints, duplicate casing conflict, and concurrent duplicate protection.
- [x] GREEN: add EF/Npgsql package references in `services/api/src/Api/Api.csproj` and Testcontainers/Npgsql test references in `services/api/tests/Integration/Integration.csproj`.
- [x] GREEN: implement `services/api/src/Api/Modules/Campaigns/Infrastructure/CampaignsDbContext.cs` and `CampaignEntityTypeConfiguration.cs` with snake_case columns, `uuid`, `varchar(120)`, `timestamptz`, generated `name_normalized`, check constraints, and unique index.
- [x] GREEN: add the EF migration under `services/api/src/Api/Modules/Campaigns/Infrastructure/Migrations/` creating only the Block 01 `campaigns` table and indexes.
- [x] GREEN: register `CampaignsDbContext` and PostgreSQL configuration in `services/api/src/Api/Program.cs`, `appsettings.json`, and `appsettings.Development.json` without adding production secrets.
- [x] TRIANGULATE: add an integration assertion that database unique/check violations are mapped without leaking constraint names or provider exception messages.
- [x] REFACTOR: keep EF Core as the persistence boundary for campaign slices and remove any speculative abstractions introduced during GREEN.

The corresponding Work Unit 2 checkboxes were updated in `openspec/changes/block-01-campaign-workspace-slice/tasks.md`.

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 2 — Backend PostgreSQL persistence and duplicate protection | Added `CampaignsPersistenceTests` first; `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignsPersistence` failed with missing `Infrastructure`, EF Core, Npgsql, Testcontainers, and `CampaignsDbContext` types. | Added EF/Npgsql/Testcontainers package references, `CampaignsDbContext`, entity configuration, migration files, connection-string configuration, and service registration. `dotnet build services/api/Lorekeeper.slnx --configuration Release` passed. | Added assertions for PostgreSQL check-constraint mapping and case-insensitive unique-violation mapping through `CampaignPersistenceErrors` without exposing constraint names or provider messages. Focused integration execution compiled but could not run because Docker was unavailable at `npipe://./pipe/docker_engine`. | EF Core remains the direct persistence boundary; no repository, MediatR, base handler, endpoint, source/chat, ingestion, auth, account, team, deletion, or AI abstractions were introduced. |

### Files changed in this update

- `services/api/src/Api/Api.csproj`
- `services/api/src/Api/Modules/Campaigns/Domain/Campaign.cs`
- `services/api/src/Api/Modules/Campaigns/Infrastructure/CampaignsDbContext.cs`
- `services/api/src/Api/Modules/Campaigns/Infrastructure/CampaignEntityTypeConfiguration.cs`
- `services/api/src/Api/Modules/Campaigns/Infrastructure/CampaignPersistenceErrors.cs`
- `services/api/src/Api/Modules/Campaigns/Infrastructure/Migrations/20260918000000_InitialCampaigns.cs`
- `services/api/src/Api/Modules/Campaigns/Infrastructure/Migrations/CampaignsDbContextModelSnapshot.cs`
- `services/api/src/Api/Program.cs`
- `services/api/src/Api/appsettings.json`
- `services/api/src/Api/appsettings.Development.json`
- `services/api/tests/Integration/Integration.csproj`
- `services/api/tests/Integration/Modules/Campaigns/CampaignsPersistenceTests.cs`
- `openspec/changes/block-01-campaign-workspace-slice/tasks.md`
- `openspec/changes/block-01-campaign-workspace-slice/apply-progress.md`

### Test commands run in this update

1. `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignsPersistence`
   - RED result: failed as expected because infrastructure and package types were missing.
2. `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignsPersistence`
   - GREEN/TRIANGULATE result: build/restore reached test execution, but Testcontainers could not connect to Docker at `npipe://./pipe/docker_engine`; all focused persistence tests were blocked by unavailable Docker.
3. `dotnet build services/api/Lorekeeper.slnx --configuration Release`
   - Result: passed with NU1903 warnings from transitive `SSH.NET` vulnerabilities introduced via Testcontainers dependencies.
4. `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignName`
   - Result: unit campaign-name tests passed, but the broad filter also matched persistence duplicate-name integration tests and those were blocked by unavailable Docker.

### Deviations from design

- No product or API behavior was changed.
- Work Unit 3 was intentionally deferred to keep the current PR boundary to PostgreSQL persistence and avoid spilling past the 500-line attempt budget.
- `dotnet ef` was not available in the environment, so the initial migration files were authored manually to match the EF model.

### Remaining tasks

Exact unchecked implementation-owned backend task lines remaining for the next slice:

- [ ] RED: add failing endpoint tests in `services/api/tests/Integration/Modules/Campaigns/CampaignEndpointsTests.cs` for `GET /api/campaigns`, `POST /api/campaigns`, `GET /api/campaigns/{campaignId}`, invalid name, duplicate name, malformed ID, unknown ID, `Location`, UTC ISO timestamps, and no implicit active campaign. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement application slices under `services/api/src/Api/Modules/Campaigns/Application/CreateCampaign/`, `ListCampaigns/`, and `GetCampaign/` using direct `CampaignsDbContext` dependencies. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement DTOs/problem mapping in `services/api/src/Api/Modules/Campaigns/CampaignEndpoints.cs` and map endpoints from `services/api/src/Api/Program.cs`. <!-- sdd-owner: implementation -->
- [ ] GREEN: return RFC 7807 Problem Details with stable `code` values `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, and `campaign_not_found`, plus field-level `name` information for name failures. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add tests proving delete, source, ingestion, chat, retrieval, citation, account, team, and implicit-active campaign endpoints are not introduced by this slice. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: ensure OpenAPI metadata names and response descriptions stay narrow to the three Block 01 campaign endpoints. <!-- sdd-owner: implementation -->

Frontend Work Units 4–7 and documentation/full-verification Work Unit 8 remain intentionally untouched in this backend slice.

### Rollback boundary

Remove the EF/Npgsql/Testcontainers package references, `CampaignsDbContext`, campaign EF configuration, persistence error mapper, migration files, connection-string registration/configuration, the EF-compatible `Campaign` property setters/constructor changes, and `CampaignsPersistenceTests`. Revert the seven Work Unit 2 checkbox updates in `tasks.md`. No HTTP endpoints, frontend code, ingestion, source lifecycle, AI, auth, account, team, or deletion behavior was introduced.

### Exact remaining unchecked implementation-owned task lines

- [ ] RED: add failing endpoint tests in `services/api/tests/Integration/Modules/Campaigns/CampaignEndpointsTests.cs` for `GET /api/campaigns`, `POST /api/campaigns`, `GET /api/campaigns/{campaignId}`, invalid name, duplicate name, malformed ID, unknown ID, `Location`, UTC ISO timestamps, and no implicit active campaign. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement application slices under `services/api/src/Api/Modules/Campaigns/Application/CreateCampaign/`, `ListCampaigns/`, and `GetCampaign/` using direct `CampaignsDbContext` dependencies. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement DTOs/problem mapping in `services/api/src/Api/Modules/Campaigns/CampaignEndpoints.cs` and map endpoints from `services/api/src/Api/Program.cs`. <!-- sdd-owner: implementation -->
- [ ] GREEN: return RFC 7807 Problem Details with stable `code` values `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, and `campaign_not_found`, plus field-level `name` information for name failures. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add tests proving delete, source, ingestion, chat, retrieval, citation, account, team, and implicit-active campaign endpoints are not introduced by this slice. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: ensure OpenAPI metadata names and response descriptions stay narrow to the three Block 01 campaign endpoints. <!-- sdd-owner: implementation -->
- [ ] RED: add failing Vitest tests under `apps/web/src/app/features/campaigns/api/campaigns-api.service.spec.ts` and `apps/web/src/app/features/campaigns/campaign-id.spec.ts` for list/create/get success, Problem Details code mapping, network/recoverable errors, and malformed UUID detection. <!-- sdd-owner: implementation -->
- [ ] GREEN: implement `apps/web/src/app/features/campaigns/models/campaign.model.ts`, `state/problem-details.ts`, `campaign-id.ts`, and `api/campaigns-api.service.ts` with only `listCampaigns`, `createCampaign`, and `getCampaign`. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add adapter tests for `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, `campaign_not_found`, and unexpected error payloads. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: keep the handwritten adapter colocated under `apps/web/src/app/features/campaigns/` and document in code/tests that generated-client automation remains out of Block 01. <!-- sdd-owner: implementation -->
- [ ] RED: add failing route/component tests in `apps/web/src/app/features/campaigns/campaign-selection.page.spec.ts` for loading, empty, list success, creation loading, creation success, field-level invalid/conflict errors, recoverable retry, and explicit Chat/Sources links. <!-- sdd-owner: implementation -->
- [ ] GREEN: register `/campaigns` in `apps/web/src/app/app.routes.ts` and implement `apps/web/src/app/features/campaigns/campaign-selection.page.ts` using Angular standalone APIs/signals. <!-- sdd-owner: implementation -->
- [ ] GREEN: update shared shell entry points in `apps/web/src/app/app.component.html`, `app.component.ts`, and `app.component.css` only as needed to host routed campaign pages and preserve existing scaffold behavior. <!-- sdd-owner: implementation -->
- [ ] GREEN: style the selection UI in `apps/web/src/styles.css` or component styles with Codex Lithographica tokens from `DESIGN.md`, English visible copy, semantic headings, visible focus, and responsive normal-flow links. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add tests proving `/campaigns` never silently enters Chat or Sources and no stored active-campaign preference overrides URL selection. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: remove any disabled-looking future actions, placeholder ingestion controls, or hidden defaults introduced while building the selection page. <!-- sdd-owner: implementation -->
- [ ] RED: add failing route/component tests in `apps/web/src/app/features/campaigns/chat-shell.page.spec.ts`, `sources-shell.page.spec.ts`, and `campaign-workspace-shell.component.spec.ts` for route loading, valid campaign context, malformed ID state, unknown ID state, and no previous-campaign fallback. <!-- sdd-owner: implementation -->
- [ ] GREEN: add `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources` to `apps/web/src/app/app.routes.ts` and implement `chat-shell.page.ts`, `sources-shell.page.ts`, and `campaign-workspace-shell.component.ts`. <!-- sdd-owner: implementation -->
- [ ] GREEN: render English Chat shell copy stating AI chat, source-backed querying, retrieval, citations, conversation history, streaming, and draft persistence are unavailable in this slice. <!-- sdd-owner: implementation -->
- [ ] GREEN: render English Sources shell copy stating ingestion, upload, paste, Notion import, update, removal, indexing, retry, progress, and source lifecycle actions are unavailable in this slice. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: add negative assertions that no query form, submit button, upload/paste/import controls, citation affordances, source-management actions, retry/progress widgets, or disabled future controls are present. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: keep campaign route loading isolated so old route data cannot display while a new `campaignId` is resolving. <!-- sdd-owner: implementation -->
- [ ] RED: add a failing Playwright spec in `apps/web/e2e/campaign-workspace.spec.ts` that opens `/campaigns`, creates `Ash Crown`, sees it listed, navigates to Chat, and navigates to Sources. <!-- sdd-owner: implementation -->
- [ ] GREEN: add only necessary E2E setup/configuration in `apps/web/e2e/playwright.config.ts` or existing test bootstrap to point at the local API/database without adding mock-only product behavior. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: assert English unavailable Chat and Sources copy in the smoke test and verify no ingestion or AI controls appear. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: keep the E2E flow focused on Block 01 and move detailed validation/error coverage back to component or integration tests. <!-- sdd-owner: implementation -->
- [ ] RED: identify any implementation-discovered spec gap in `openspec/changes/block-01-campaign-workspace-slice/proposal.md`, `specs/campaigns/spec.md`, `specs/workspace/spec.md`, or `design.md` before changing behavior. <!-- sdd-owner: implementation -->
- [ ] GREEN: update `docs/10-roadmap.md` Block 01 readiness/exit evidence and any affected command/status notes only after behavior and tests exist. <!-- sdd-owner: implementation -->
- [ ] GREEN: update `docs/06-api-contracts.md`, `docs/03-domain-model.md`, or `docs/12-workspace-and-ingestion-ux.md` only if implementation reveals a documented Block 01 gap, keeping PRODUCT.md behavior unchanged unless the spec is explicitly changed first. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: run `pnpm run test`, `pnpm run build`, `dotnet build services/api/Lorekeeper.slnx --configuration Release`, and `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` as the apply gate and record any deviations. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: run `pnpm run lint`, `pnpm run test`, `pnpm run build`, `pnpm run e2e`, `dotnet format services/api/Lorekeeper.slnx --verify-no-changes`, `dotnet build services/api/Lorekeeper.slnx --no-restore --configuration Release`, and `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` as the verify gate. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: remove dead code, unused test helpers, unverified placeholders, generated artifacts not required for Block 01, and any accidental out-of-scope affordances before review. <!-- sdd-owner: implementation -->

## Work Unit 3 apply — Backend campaign HTTP API

### Scope

Implemented the backend-only HTTP API slice on `feat/block-01-http-api`. Frontend, E2E, ingestion, source lifecycle, AI, auth, accounts, teams, and deletion remain out of scope.

### Completed tasks and persisted checkboxes

- [x] RED: add failing endpoint tests in `services/api/tests/Integration/Modules/Campaigns/CampaignEndpointsTests.cs` for `GET /api/campaigns`, `POST /api/campaigns`, `GET /api/campaigns/{campaignId}`, invalid name, duplicate name, malformed ID, unknown ID, `Location`, UTC ISO timestamps, and no implicit active campaign.
- [x] GREEN: implement application slices under `services/api/src/Api/Modules/Campaigns/Application/CreateCampaign/`, `ListCampaigns/`, and `GetCampaign/` using direct `CampaignsDbContext` dependencies.
- [x] GREEN: implement DTOs/problem mapping in `services/api/src/Api/Modules/Campaigns/CampaignEndpoints.cs` and map endpoints from `services/api/src/Api/Program.cs`.
- [x] GREEN: return RFC 7807 Problem Details with stable `code` values `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, and `campaign_not_found`, plus field-level `name` information for name failures.
- [x] TRIANGULATE: add tests proving delete, source, ingestion, chat, retrieval, citation, account, team, and implicit-active campaign endpoints are not introduced by this slice.
- [x] REFACTOR: ensure OpenAPI metadata names and response descriptions stay narrow to the three Block 01 campaign endpoints.

### Evidence

1. `dotnet build services/api/Lorekeeper.slnx --configuration Release`
   - Result: passed with NU1903 warnings from transitive `SSH.NET` vulnerabilities introduced via Testcontainers dependencies.
2. `dotnet format services/api/Lorekeeper.slnx --verify-no-changes`
   - Result: passed with workspace-load warnings.
3. `dotnet test services/api/tests/Unit/Unit.csproj --no-build --configuration Release --filter FullyQualifiedName~Campaign`
   - Result: passed, 18 tests.
4. `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release --filter FullyQualifiedName~CampaignEndpoints`
   - Result: focused endpoint tests were discovered, but execution is blocked locally because Testcontainers cannot connect to Docker at `npipe://./pipe/docker_engine`.

### Architecture note

`Program.cs` remains a thin composition root. Campaign service registration and endpoint mapping are routed through `CampaignsModule` (`AddCampaignsModule` / `MapCampaignsModule`) so domain, application, infrastructure, and HTTP adapter concerns remain separated without adding MediatR, repositories, or framework ceremony.

### Remaining tasks

Frontend Work Units 4–7 and documentation/full-verification Work Unit 8 remain intentionally untouched. Docker-backed endpoint tests must run in CI or another environment with Docker available before this slice is considered fully verified.

## Work Unit 4 apply — Frontend campaign API adapter and typed state

### Scope

Implemented the frontend-only handwritten campaign API adapter slice on `feat/block-01-frontend-adapter`. Campaign selection UI, workspace routes, Chat/Sources shells, E2E, and docs remain out of scope.

### Completed tasks and persisted checkboxes

- [x] RED: add failing Vitest tests under `apps/web/src/app/features/campaigns/api/campaigns-api.service.spec.ts` and `apps/web/src/app/features/campaigns/campaign-id.spec.ts` for list/create/get success, Problem Details code mapping, network/recoverable errors, and malformed UUID detection.
- [x] GREEN: implement `apps/web/src/app/features/campaigns/models/campaign.model.ts`, `state/problem-details.ts`, `campaign-id.ts`, and `api/campaigns-api.service.ts` with only `listCampaigns`, `createCampaign`, and `getCampaign`.
- [x] TRIANGULATE: add adapter tests for `campaign_name_invalid`, `campaign_name_conflict`, `campaign_id_invalid`, `campaign_not_found`, and unexpected error payloads.
- [x] REFACTOR: keep the handwritten adapter colocated under `apps/web/src/app/features/campaigns/` and document in code/tests that generated-client automation remains out of Block 01.

### Evidence

1. `pnpm --filter web test -- --run campaigns`
   - Result: blocked by Angular builder argument schema handling: `Option '--' has been specified multiple times` and `Data path "" must NOT have additional properties()`.
2. `pnpm --filter web test`
   - Result: passed, 3 files and 11 tests.
3. `pnpm --filter web lint`
   - Result: passed.
4. `pnpm --filter web build`
   - Result: passed.

### Deviations

- The planned focused command did not work with the current Angular unit-test builder invocation. The full frontend test suite was run instead and passed.

### Remaining tasks

Frontend route/page Work Units 5–6, E2E Work Unit 7, and docs/full-verification Work Unit 8 remain intentionally untouched.

## Work Unit 5 apply — Frontend campaign selection page

### Scope

Implemented the `/campaigns` route and campaign selection/creation page on `feat/block-01-frontend-ui`. Chat and Sources workspace shells, E2E, docs, and final verification remain out of scope.

### Completed tasks and persisted checkboxes

- [x] RED: add failing route/component tests in `apps/web/src/app/features/campaigns/pages/campaign-selection/campaign-selection.page.spec.ts` for loading, empty, list success, creation loading, creation success, field-level invalid/conflict errors, recoverable retry, and explicit Chat/Sources links.
- [x] GREEN: register `/campaigns` in `apps/web/src/app/app.routes.ts` and implement `apps/web/src/app/features/campaigns/pages/campaign-selection/campaign-selection.page.ts` using Angular standalone APIs/signals.
- [x] GREEN: update shared shell entry points in `apps/web/src/app/app.component.html`, `app.component.ts`, and `app.component.css` only as needed to host routed campaign pages and preserve existing scaffold behavior.
- [x] GREEN: style the selection UI in component styles with Codex Lithographica tokens from `DESIGN.md`, English visible copy, semantic headings, visible focus, and responsive normal-flow links.
- [x] TRIANGULATE: add tests proving `/campaigns` never silently enters Chat or Sources and no stored active-campaign preference overrides URL selection.
- [x] REFACTOR: remove any disabled-looking future actions, placeholder ingestion controls, or hidden defaults introduced while building the selection page.

### Evidence

Design review: pass
Checked against: `DESIGN.md`, `docs/conventions/frontend-design-review.md`
Notes: no blockers; follow-ups may tighten canonical font stacks, aria-live announcements, and date formatting.

1. `pnpm --filter web format:check`
   - Result: passed.
2. `pnpm --filter web lint`
   - Result: passed.
3. `pnpm --filter web build`
   - Result: passed.
4. `pnpm --filter web test`
   - Result: passed, 4 files and 16 tests.

### Architecture note

The campaign feature is now organized by responsibility: `api/`, `models/`, `routing/`, `state/`, and `pages/campaign-selection/`. API adapters remain Promise-based; page reads use Angular `resource`; UI commands wrap adapter promises with RxJS `from(...)`, `finalize(...)`, and `takeUntilDestroyed(...)`.

### Remaining tasks

Frontend workspace shell Work Unit 6, E2E Work Unit 7, and docs/full-verification Work Unit 8 remain intentionally untouched.

---

## Work Unit 6 apply — Frontend Chat and Sources workspace shells

### Scope

Implemented the frontend-only workspace route shell slice on `feat/block-01-workspace-shells`. The routes `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources` now resolve campaign context from the URL, validate malformed IDs before API calls, and render unavailable-capability shells. AI chat, source-backed querying, ingestion, source lifecycle, retrieval, citations, history, streaming, draft persistence, auth, accounts, teams, and deletion remain out of scope.

### Completed tasks and persisted checkboxes

- [x] RED: add failing route/component tests in `apps/web/src/app/features/campaigns/chat-shell.page.spec.ts`, `sources-shell.page.spec.ts`, and `campaign-workspace-shell.component.spec.ts` for route loading, valid campaign context, malformed ID state, unknown ID state, and no previous-campaign fallback.
- [x] GREEN: add `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources` to `apps/web/src/app/app.routes.ts` and implement `chat-shell.page.ts`, `sources-shell.page.ts`, and `campaign-workspace-shell.component.ts`.
- [x] GREEN: render English Chat shell copy stating AI chat, source-backed querying, retrieval, citations, conversation history, streaming, and draft persistence are unavailable in this slice.
- [x] GREEN: render English Sources shell copy stating ingestion, upload, paste, Notion import, update, removal, indexing, retry, progress, and source lifecycle actions are unavailable in this slice.
- [x] TRIANGULATE: add negative assertions that no query form, submit button, upload/paste/import controls, citation affordances, source-management actions, retry/progress widgets, or disabled future controls are present.
- [x] REFACTOR: keep campaign route loading isolated so old route data cannot display while a new `campaignId` is resolving.

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 6 — Frontend Chat and Sources workspace shells | Added route/page/component tests for Chat, Sources, malformed route IDs, unknown/recoverable campaign states, valid campaign context loading, and previous-campaign fallback prevention before final shell implementation. | Added `ChatShellPage`, `SourcesShellPage`, `CampaignWorkspaceShellComponent`, and route entries for `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources`. | Tests assert unavailable Chat and Sources copy and verify no query form, submit action, upload/paste/import controls, citation affordances, or disabled future controls appear. | Campaign display is guarded by matching the loaded campaign ID to the current route ID, preventing old campaign data from showing while a new `campaignId` is resolving. New shell styling uses Tailwind utilities instead of page-local CSS. |

### Files changed in this update

- `apps/web/src/app/app.routes.ts`
- `apps/web/src/app/features/campaigns/pages/campaign-workspace-shell/campaign-workspace-shell.component.html`
- `apps/web/src/app/features/campaigns/pages/campaign-workspace-shell/campaign-workspace-shell.component.spec.ts`
- `apps/web/src/app/features/campaigns/pages/campaign-workspace-shell/campaign-workspace-shell.component.ts`
- `apps/web/src/app/features/campaigns/pages/chat-shell/chat-shell.page.html`
- `apps/web/src/app/features/campaigns/pages/chat-shell/chat-shell.page.spec.ts`
- `apps/web/src/app/features/campaigns/pages/chat-shell/chat-shell.page.ts`
- `apps/web/src/app/features/campaigns/pages/sources-shell/sources-shell.page.html`
- `apps/web/src/app/features/campaigns/pages/sources-shell/sources-shell.page.spec.ts`
- `apps/web/src/app/features/campaigns/pages/sources-shell/sources-shell.page.ts`
- `docs/conventions/frontend.md`
- `openspec/changes/block-01-campaign-workspace-slice/tasks.md`
- `openspec/changes/block-01-campaign-workspace-slice/apply-progress.md`

### Test commands run in this update

1. `pnpm --filter web test`
   - Result: passed; 7 test files / 23 tests.
2. `pnpm --filter web format:check`
   - Result: passed.
3. `pnpm --filter web lint`
   - Result: passed.
4. `pnpm --filter web build`
   - Result: passed.

### Deviations from design

- Design review gate: pass for this slice boundary. The shells use existing Codex Lithographica tokens through Tailwind utilities, semantic headings, visible status/error states, and normal-flow navigation back to campaigns.
- No product behavior was expanded beyond unavailable workspace shells.
- The temporary `workspace-unavailable` route placeholder from the previous review fix was removed and replaced with the real Work Unit 6 route shells.
- A frontend convention was added to make Tailwind the default for feature UI styling; broader reusable component/design-system repairs are intentionally tracked in issue #8 and not solved in this work unit.

### Remaining tasks

Exact unchecked implementation-owned task lines remaining for the next slice:

- [ ] RED: add a failing Playwright spec in `apps/web/e2e/campaign-workspace.spec.ts` that opens `/campaigns`, creates `Ash Crown`, sees it listed, navigates to Chat, and navigates to Sources. <!-- sdd-owner: implementation -->
- [ ] GREEN: add only necessary E2E setup/configuration in `apps/web/e2e/playwright.config.ts` or existing test bootstrap to point at the local API/database without adding mock-only product behavior. <!-- sdd-owner: implementation -->
- [ ] TRIANGULATE: assert English unavailable Chat and Sources copy in the smoke test and verify no ingestion or AI controls appear. <!-- sdd-owner: implementation -->
- [ ] REFACTOR: keep the E2E flow focused on Block 01 and move detailed validation/error coverage back to component or integration tests. <!-- sdd-owner: implementation -->

### Rollback boundary

Remove the Chat/Sources route entries, `chat-shell`, `sources-shell`, and `campaign-workspace-shell` page/component/test files, and restore or remove the previous temporary workspace placeholder as needed. Revert the six Work Unit 6 checkbox updates in `tasks.md` and this Work Unit 6 section in `apply-progress.md`.

---

## Work Unit 7 apply — Focused E2E smoke flow (blocked)

### Structured status consumed

- Native status schema: `gentle-ai.sdd-status` v2
- Change: `block-01-campaign-workspace-slice`
- Apply state on entry: `ready`
- Artifact store: `openspec`
- Allowed edit root: repository root
- Branch: `feat/block-01-e2e-docs`

### Workload / PR boundary

- Delivery strategy: `auto-chain`
- Chain strategy: `feature-branch-chain`
- Current slice: Work Unit 7 only, intended as the E2E/docs child boundary.

### Work Unit Evidence

| Evidence | Result |
| --- | --- |
| Focused test command and exact result | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` failed before the test ran because the API web server could not build. The installed .NET SDK is `8.0.131`, while `services/api/src/Api/Api.csproj` targets `net10.0` (`NETSDK1045`). |
| Runtime harness command/scenario and exact result | The same Playwright command starts the local API at `http://127.0.0.1:5044/health` and Angular app at `http://127.0.0.1:4200`. It was blocked at API compilation. `docker version --format '{{.Server.Version}}'` also failed because Docker Desktop's Linux engine pipe was unavailable, so PostgreSQL could not be prepared for the real API boundary. |
| Rollback boundary | Remove `apps/web/e2e/campaign-workspace.spec.ts` and restore `apps/web/e2e/playwright.config.ts`. No product code, API behavior, database schema, mock behavior, or documentation outside this progress record changed. |

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | Added `apps/web/e2e/campaign-workspace.spec.ts` before changing E2E setup. It visits `/campaigns`, submits `Ash Crown`, asserts the campaign is listed, and follows Chat and Sources navigation. The initial focused attempt was blocked by the existing pnpm launcher/version mismatch; after the minimal API/frontend Playwright web-server setup was added, the test harness reached the API startup boundary and failed because .NET 10 is unavailable. | Blocked — the local API cannot compile with SDK `8.0.131`; no passing E2E result exists. | Not attempted — the unavailable Chat/Sources copy and absent-control assertions must be added only after the base flow reaches GREEN. | Not attempted — no passing behavior exists to refactor. |

### Files changed in this update

- `apps/web/e2e/campaign-workspace.spec.ts`
- `apps/web/e2e/playwright.config.ts`
- `openspec/changes/block-01-campaign-workspace-slice/apply-progress.md`

### Additional frontend checks

1. `corepack pnpm@12.4.1 --filter web exec prettier --check e2e/campaign-workspace.spec.ts e2e/playwright.config.ts`
   - Result: passed after normalizing `playwright.config.ts` with Prettier.
2. `corepack pnpm@12.4.1 --filter web lint`
   - Result: passed.
3. `corepack pnpm@12.4.1 --filter web build`
   - Result: passed.

### Blockers

1. Install/use a .NET 10 SDK so `services/api/src/Api/Api.csproj` can build and start.
2. Start Docker Desktop's Linux engine so PostgreSQL can run locally; then apply the existing campaign migration before re-running the E2E flow.
3. Re-run the RED-to-GREEN cycle, add the unavailable-capability and absent-control assertions, then perform the focused E2E/frontend checks before marking any Work Unit 7 checkboxes complete.

### Task state

All four Work Unit 7 task checkboxes remain unchecked. No commit was created because the required focused E2E and runtime harness did not pass.

---

## Work Unit 7 apply — resumed focused E2E smoke flow (blocked)

### Scope

Resumed only Work Unit 7 on `feat/block-01-e2e-docs`. The existing Playwright RED spec and E2E configuration were exercised against the local API and Angular servers; no product, API, database, or future-scope behavior changed.

### Work Unit Evidence

| Evidence | Result |
| --- | --- |
| Focused test command and exact result | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` was run three times. Attempt 1 failed before test execution because the API project path was resolved from the Playwright config directory. Attempt 2 started the API but failed starting Angular because `corepack pnpm@12.4.1 run start -- --host 127.0.0.1` forwarded an unsupported extra `--` to `ng serve`. Attempt 3 started both web servers and reached Chromium launch, then failed because Playwright's Chromium headless-shell executable is absent at `C:\Users\Usuario\AppData\Local\ms-playwright\chromium_headless_shell-1243\chrome-headless-shell-win64\chrome-headless-shell.exe`. The smoke assertions did not run. |
| Runtime harness command/scenario and exact result | The same focused command started the real API with .NET SDK `10.0.401`, passed its `http://127.0.0.1:5044/health` readiness check, and started Angular at `http://127.0.0.1:4200` with the local `/api` proxy. It stopped before a browser could create `Ash Crown` against PostgreSQL because Chromium is not installed. |
| Rollback boundary | Revert `apps/web/e2e/playwright.config.ts` and remove `apps/web/e2e/campaign-workspace.spec.ts`; remove this resumed-progress section. No application, API, schema, mock, or documentation behavior outside Work Unit 7 changed. |

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | The existing RED spec was written before its E2E configuration. This resumed run confirmed its base flow cannot execute until the harness can launch Chromium. | Blocked — corrected the config-only API working directory to `../../..` and restored the Angular command to the existing `pnpm run start --host 127.0.0.1` form; both servers then reached readiness, but Chromium is unavailable. | Not attempted — English unavailable-copy and absent-control assertions must be added only after the base flow reaches GREEN in a real browser. | Not attempted — no passing smoke behavior exists to refactor. |

### Current blockers

1. Playwright Chromium is not installed locally. The runner explicitly requests `pnpm exec playwright install`; downloading that browser requires user authorization for the remote download.
2. The prior .NET SDK path blocker is resolved: the API web server started using SDK `10.0.401`. PostgreSQL-backed create/navigation behavior remains unobserved until Chromium is available.

### Task state

All four Work Unit 7 task checkboxes remain unchecked. No commit was created.

---

## Work Unit 7 final status — completed

This final status supersedes the earlier blocked Work Unit 7 attempts above. Lorekeeper local development now defaults Compose PostgreSQL and the Development API connection string to host port `5433`; `POSTGRES_PORT` remains an override. Only `lorekeeper-postgres` was recreated with `docker compose up -d --force-recreate --no-deps postgres`; its named `lorekeeper_postgres-data` volume was retained, migrations applied, and the external Windows PostgreSQL service was untouched.

| Evidence | Observed final result |
| --- | --- |
| Focused E2E | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` — passed, 1 passed. |
| API-to-container proof | After deleting the prior test row directly from `lorekeeper-postgres`, the E2E-created `Ash Crown` row was returned by `docker exec lorekeeper-postgres psql ... SELECT name FROM campaigns WHERE name = 'Ash Crown';`. |
| Migration | `corepack pnpm@12.4.1 run api:migrate` — built successfully and applied `20260918000000_InitialCampaigns`. |
| Frontend checks | Targeted Prettier check, `pnpm --filter web lint`, `pnpm --filter web build`, and `pnpm --filter web test` all passed; the unit suite reported 7 files and 23 tests. |

| Work unit | RED | GREEN | TRIANGULATE | REFACTOR |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | The Playwright smoke and an explicit port-5433 configuration assertion failed before the local-default repair. | The compose/API defaults were changed to `5433`, the container was healthy, migrations applied, and the browser smoke passed. | The smoke now asserts the exact English unavailable copy for Chat and Sources; Chat has no textbox or button and Sources has no file input or button. | No production refactor was necessary; the smoke remains focused and detailed error coverage stays below E2E. |

All four Work Unit 7 checkboxes are `[x]`; Work Unit 8 remains unchanged.

### Local commit result

`git commit -m "test(e2e): complete campaign workspace smoke flow"` was attempted after the focused E2E and frontend checks passed. The repository pre-commit hook blocked the commit because its broad `pnpm --filter web format:check` found pre-existing Prettier violations in 43 unrelated files. No commit was created, and those unrelated files were not reformatted or changed in this work unit.

---

## Work Unit 7 apply — local PostgreSQL port repair and completed focused E2E smoke flow

### Scope

Continued only Work Unit 7 on `feat/block-01-e2e-docs`. The verified host-port collision with the external Windows PostgreSQL service was repaired in Lorekeeper's local-development defaults only: Compose now maps its PostgreSQL container to host port `5433` by default and the Development API connection string uses `5433`. `POSTGRES_PORT` remains a Compose override, and `ConnectionStrings__Campaigns` remains the standard .NET configuration override. The external service was not stopped or modified, and the named Lorekeeper volume was preserved.

### Completed tasks and persisted checkboxes

- [x] RED: add a failing Playwright spec in `apps/web/e2e/campaign-workspace.spec.ts` that opens `/campaigns`, creates `Ash Crown`, sees it listed, navigates to Chat, and navigates to Sources.
- [x] GREEN: add only necessary E2E setup/configuration in `apps/web/e2e/playwright.config.ts` or existing test bootstrap to point at the local API/database without adding mock-only product behavior.
- [x] TRIANGULATE: assert English unavailable Chat and Sources copy in the smoke test and verify no ingestion or AI controls appear.
- [x] REFACTOR: keep the E2E flow focused on Block 01 and move detailed validation/error coverage back to component or integration tests.

### Work Unit Evidence

| Evidence | Result |
| --- | --- |
| Focused test command and exact result | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` passed: 1 passed. The real Chromium flow created `Ash Crown`, displayed it in the campaign list, and navigated to both workspace shells. |
| Runtime harness command/scenario and exact result | `docker compose up -d --force-recreate --no-deps postgres` recreated only `lorekeeper-postgres`; `docker compose ps postgres` showed `0.0.0.0:5433->5432/tcp` and healthy status; `docker volume inspect lorekeeper_postgres-data --format '{{.Name}}'` returned `lorekeeper_postgres-data`. `corepack pnpm@12.4.1 run api:migrate` built successfully and applied `20260918000000_InitialCampaigns`. Before the final E2E execution, `docker exec lorekeeper-postgres psql ... -c "DELETE FROM campaigns WHERE name = 'Ash Crown';"` returned `DELETE 1`; afterward `docker exec lorekeeper-postgres psql ... -tAc "SELECT name FROM campaigns WHERE name = 'Ash Crown';"` returned `Ash Crown`, proving the API used the recreated Lorekeeper container. |
| Rollback boundary | Revert `docker-compose.yml`, `services/api/src/Api/appsettings.Development.json`, `apps/web/e2e/campaign-workspace.spec.ts`, `apps/web/e2e/playwright.config.ts`, the four Work Unit 7 checkboxes, and this progress section. The named volume remains intact; test-only `Ash Crown` data can be removed from `lorekeeper-postgres` without deleting the volume. |

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | The existing Playwright smoke had already failed at real campaign creation while Lorekeeper targeted the external port-5432 PostgreSQL. A configuration assertion expecting Compose publication and the Development connection string to use `5433` failed before changing either file. | Changed only the Compose default mapping from `${POSTGRES_PORT:-5432}` to `${POSTGRES_PORT:-5433}` and the Development connection string from `Port=5432` to `Port=5433`; the configuration assertion passed, migrations applied, and the focused Playwright flow passed. | Added English Chat/Sources unavailable-capability assertions plus real absent-control assertions: Chat has no textbox or button; Sources has no file input or button. The focused Playwright flow passed after adding them. | No production refactor was needed. The E2E remains one focused operator flow; detailed validation and route-state coverage remains in existing component/integration tests. |

### Configuration checks

1. Configuration RED assertion (before the port change): failed as expected with `Expected Lorekeeper development defaults to use host port 5433.`
2. Configuration GREEN assertion (after the port change): passed by confirming `docker compose config` published `5433` and `appsettings.Development.json` contained `Port=5433`.
3. `POSTGRES_PORT=6543 docker compose config` equivalent environment assertion: passed with `POSTGRES_PORT override preserved`.

### Frontend checks

1. `corepack pnpm@12.4.1 --filter web exec prettier --check e2e/campaign-workspace.spec.ts e2e/playwright.config.ts`
   - Result: passed; all matched files use Prettier code style.
2. `corepack pnpm@12.4.1 --filter web lint`
   - Result: passed; all files pass linting.
3. `corepack pnpm@12.4.1 --filter web build`
   - Result: passed; Angular application bundle generated successfully.
4. `corepack pnpm@12.4.1 --filter web test`
   - Result: passed; 7 test files and 23 tests.

### Files changed in this update

- `docker-compose.yml`
- `services/api/src/Api/appsettings.Development.json`
- `apps/web/e2e/campaign-workspace.spec.ts`
- `apps/web/e2e/playwright.config.ts`
- `openspec/changes/block-01-campaign-workspace-slice/tasks.md`
- `openspec/changes/block-01-campaign-workspace-slice/apply-progress.md`

### Task state

All four Work Unit 7 subtasks are complete and verified. No Work Unit 8 task was changed.

## Work Unit 7 apply — resumed after Chromium installation (blocked)

### Scope

Resumed only Work Unit 7 on `feat/block-01-e2e-docs`. Chromium was available and the existing focused E2E smoke was run against the real Angular, API, and PostgreSQL boundary. No production, configuration, or task-checkbox changes were made.

### Work Unit Evidence

| Evidence | Result |
| --- | --- |
| Focused test command and exact result | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` started Angular and Chromium, then failed at `apps/web/e2e/campaign-workspace.spec.ts:10`: `Ash Crown` never appeared in the campaign list. The rendered UI showed both `Campaign creation failed. Check the connection and retry.` and `Campaigns could not be loaded. Retry when the API is available.` |
| Runtime harness command/scenario and exact result | `docker compose ps postgres` reported `lorekeeper-postgres` healthy on port `5432`. The Playwright API and Angular web servers reached readiness, but the browser rendered campaign list and creation recoverable-error states. |
| Migration setup command and exact result | `corepack pnpm@12.4.1 run api:migrate` failed: `Ejecute "dotnet tool restore" para que esté disponible el comando "dotnet-ef".` The repository manifest declares `dotnet-ef` version `10.0.0`; its restore has not been run in this environment. |
| Rollback boundary | Remove `apps/web/e2e/campaign-workspace.spec.ts`, restore `apps/web/e2e/playwright.config.ts`, and remove this progress section. No application, API, schema, mock, or documentation behavior outside Work Unit 7 changed. |

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | The existing RED smoke spec executed in Chromium and failed at the real campaign creation/list boundary. | Blocked — the local database migration cannot run until the repository's `dotnet-ef` tool is restored. | Not attempted — English unavailable-copy and absent-control assertions remain gated on a passing base flow. | Not attempted — no passing smoke behavior exists to refactor. |

### Current blocker

`dotnet tool restore` may access NuGet to install the manifest-pinned `dotnet-ef` tool. Chromium authorization does not authorize that separate remote package operation, so the migration and focused E2E GREEN step cannot proceed without explicit NuGet restore authorization.

### Task state

All four Work Unit 7 task checkboxes remain unchecked. No commit was created.

---

## Work Unit 7 apply — resumed after dotnet-ef restore (blocked)

### Scope

Resumed only Work Unit 7 on `feat/block-01-e2e-docs`. The manifest-pinned `dotnet-ef` 10.0.0 tool was already restored before this run. The repository migration command and existing real Playwright smoke were executed against the local services. No source, E2E configuration, database schema, or task-checkbox changes were made.

### Work Unit Evidence

| Evidence | Result |
| --- | --- |
| Focused test command and exact result | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` started Angular and executed Chromium. It failed at `apps/web/e2e/campaign-workspace.spec.ts:10` because the `Ash Crown` campaign list item was not visible within 5 seconds. Result: 1 failed. |
| Runtime harness command/scenario and exact result | The focused Playwright command ran the existing real browser flow against the local Angular and API web-server setup. Campaign creation/list behavior did not reach a passing result because the local database migration remains unapplied. |
| Migration setup command and exact result | `corepack pnpm@12.4.1 run api:migrate` invoked `dotnet tool run dotnet-ef database update --project services/api/src/Api/Api.csproj --startup-project services/api/src/Api/Api.csproj`. The API project built successfully, then the database connection failed with PostgreSQL `28P01` password authentication failure for user `lorekeeper`. Result: exit code 1; no migration was applied. |
| Rollback boundary | No implementation files changed in this resumed attempt. The only durable update is this observed-evidence section in `openspec/changes/block-01-campaign-workspace-slice/apply-progress.md`. |

### TDD Cycle Evidence

| Work unit | RED evidence | GREEN evidence | TRIANGULATE evidence | REFACTOR evidence |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | The existing RED Playwright spec executed in Chromium and failed at the real campaign creation/list assertion. | Blocked — `dotnet-ef` is available, but the configured local `lorekeeper` PostgreSQL credentials were rejected before migrations could be applied. | Not attempted — unavailable-capability and absent-control assertions remain gated on a passing base flow. | Not attempted — no passing smoke behavior exists to refactor. |

### Current blocker

The healthy local `lorekeeper-postgres` instance rejects the development connection string credentials configured for the `lorekeeper` user. Align the local database credentials with `services/api/src/Api/appsettings.Development.json`, then re-run the repository migration command and focused Playwright flow. Do not mark Work Unit 7 complete until that flow passes and the required unavailable-capability and absent-control assertions have been added and verified.

### Historical task state

All four Work Unit 7 task checkboxes remained unchecked at this point. No commit was created.

---

## Work Unit 7 final status — completed

This final status supersedes the earlier blocked Work Unit 7 attempts above. Lorekeeper local development now defaults Compose PostgreSQL and the Development API connection string to host port `5433`; `POSTGRES_PORT` remains an override. Only `lorekeeper-postgres` was recreated with `docker compose up -d --force-recreate --no-deps postgres`; its named `lorekeeper_postgres-data` volume was retained, migrations applied, and the external Windows PostgreSQL service was untouched.

| Evidence | Observed final result |
| --- | --- |
| Focused E2E | `corepack pnpm@12.4.1 --filter web exec playwright test e2e/campaign-workspace.spec.ts --config e2e/playwright.config.ts` — passed, 1 passed. |
| API-to-container proof | After deleting the prior test row directly from `lorekeeper-postgres`, the E2E-created `Ash Crown` row was returned by `docker exec lorekeeper-postgres psql ... SELECT name FROM campaigns WHERE name = 'Ash Crown';`. |
| Migration | `corepack pnpm@12.4.1 run api:migrate` — built successfully and applied `20260918000000_InitialCampaigns`. |
| Frontend checks | Targeted Prettier check, `pnpm --filter web lint`, `pnpm --filter web build`, and `pnpm --filter web test` all passed; the unit suite reported 7 files and 23 tests. |

| Work unit | RED | GREEN | TRIANGULATE | REFACTOR |
| --- | --- | --- | --- | --- |
| Work Unit 7 — Focused E2E smoke flow | The Playwright smoke and an explicit port-5433 configuration assertion failed before the local-default repair. | The compose/API defaults were changed to `5433`, the container was healthy, migrations applied, and the browser smoke passed. | The smoke now asserts the exact English unavailable copy for Chat and Sources; Chat has no textbox or button and Sources has no file input or button. | No production refactor was necessary; the smoke remains focused and detailed error coverage stays below E2E. |

All four Work Unit 7 checkboxes are `[x]`; Work Unit 8 remains unchanged.
