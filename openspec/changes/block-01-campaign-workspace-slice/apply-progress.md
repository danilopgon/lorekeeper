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
