# Proposal — Block 01 Campaign Workspace Slice

## Intent

Implement Lorekeeper's first product slice: an explicit campaign selection boundary backed by PostgreSQL, API endpoints, Angular routes, and empty campaign workspace shells for Chat and Sources.

This change gives the personal operator a real campaign context before any future ingestion, retrieval, AI chat, citations, or source lifecycle behavior exists. It prevents hidden defaults by making the active campaign the URL-scoped campaign identifier, not a session preference or implicit server state.

## Problem statement

Lorekeeper currently has scaffold and CI foundation, but no usable first product flow. Future notes, sources, retrieval, and answers all require a campaign boundary. Without persisted campaigns and URL-scoped workspace routes, later features would either lack a domain anchor or be tempted to rely on implicit defaults that can leak context between campaigns.

## Scope

### In scope

- Persisted `Campaign` entity with:
  - system-generated opaque `id`;
  - trimmed `name`;
  - server-owned `createdAt` and `updatedAt` timestamps.
- Campaign-name validation:
  - required after trimming;
  - 1–120 characters after trimming;
  - case-insensitive uniqueness across the personal installation.
- PostgreSQL persistence through the .NET API.
- Database enforcement of case-insensitive campaign-name uniqueness, not only application-level validation.
- API endpoints under `/api`:
  - `GET /api/campaigns`;
  - `POST /api/campaigns`;
  - `GET /api/campaigns/{campaignId}`.
- RFC 7807 Problem Details for expected failures with stable public `code` extensions.
- Angular routes:
  - `/campaigns`;
  - `/campaigns/{campaignId}/chat`;
  - `/campaigns/{campaignId}/sources`.
- Campaign selection and creation UI states:
  - loading;
  - empty;
  - validation error;
  - recoverable API error;
  - success.
- Chat workspace shell for a selected campaign that clearly states AI chat and sources are not available in this slice.
- Sources workspace shell for a selected campaign that clearly states ingestion, upload, paste, import, update, and removal actions are not available in this slice.
- Unknown or malformed campaign routes that report validation/not-found state without falling back to another campaign.
- A small handwritten Angular API adapter is allowed for Block 01 if generated client automation is not already present; the generated client workflow remains owned by Block 02.
- Verification covering domain invariants, API behavior, route states, and one focused E2E smoke flow.

### Out of scope

- Ingestion, uploads, pasted text, Notion import, indexing, retrieval, citations, source lifecycle, source update, source removal, or source progress.
- AI chat, grounded answer generation, conversation history, streaming, draft persistence, or provider integration.
- Campaign deletion, archival, soft delete, hard delete, or cascade policy.
- Accounts, teams, invitations, multiuser permissions, authentication, authorization, or Internet-exposure access control.
- Cross-campaign search or any implicit active-campaign endpoint.
- Full OpenAPI generated TypeScript client automation unless existing repository support makes it trivial without expanding Block 01.

## Affected areas

- Backend domain/application layer for `Campaign` creation, listing, lookup, validation, and conflict handling.
- Backend infrastructure for EF Core/PostgreSQL schema, migrations, timestamp handling, and case-insensitive uniqueness enforcement.
- Backend HTTP/API surface for campaign DTOs, endpoint routing, status codes, Problem Details, and OpenAPI metadata.
- Angular campaign-selection page and campaign workspace route shells.
- Angular route guards/resolvers or equivalent route-loading logic for malformed, missing, and unknown campaign IDs.
- Angular service/API adapter for Block 01 campaign calls.
- Frontend design implementation aligned to `DESIGN.md` Codex Lithographica tokens, accessible states, keyboard use, and responsive behavior.
- Test suites for backend unit/integration coverage, frontend route/component coverage, and a focused Playwright smoke flow.
- Documentation/spec artifacts for this SDD change; product scope documents remain authoritative and should only be updated if later phases identify an explicit spec gap.

## Design and planning decisions

- Campaign IDs remain opaque in public contracts. The implementation may choose a concrete generated identifier format during design, but malformed IDs must map to `campaign_id_invalid` and must not resolve to another campaign.
- Unknown campaign IDs map to `campaign_not_found` and must render a not-found state in workspace routes.
- The active campaign is always represented by the URL. `/campaigns` may list campaigns and allow selection, but it must not silently enter Chat or Sources without an explicit campaign route.
- PostgreSQL must enforce case-insensitive campaign-name uniqueness. The design phase should choose the exact schema/index approach, such as a normalized-name column, expression index, or `citext`, and document tradeoffs.
- Angular may use a small handwritten API adapter for this slice. Generated OpenAPI client workflow remains Block 02 unless existing generation is already present and can be used without broadening scope.
- Empty Chat and Sources shells should communicate future unavailable capabilities without rendering disabled controls that look usable.
- Visible application UI copy for Block 01 is English. Documentation and design prose may remain in their existing language, but implemented labels, states, errors, and test assertions for user-visible UI text should use English.
- Copy should stay minimal and product-honest: Chat has no AI/source-backed query capability yet; Sources has no ingestion/source-management capability yet.

## Risks

- **Identifier ambiguity:** If the chosen campaign ID format is not validated consistently, malformed routes may be treated as unknown IDs or accidentally fall through to another state.
- **Uniqueness race:** Application-only duplicate checks can race; database-level case-insensitive uniqueness is required.
- **Scope creep:** Chat and Sources shells could accidentally introduce ingestion, upload, query, or source-management affordances before their blocks are ready.
- **Client contract drift:** A handwritten adapter could diverge from API contracts if Block 02 generated-client work is not kept as the next contract-hardening step.
- **Review size:** This slice crosses backend persistence, API, frontend routes, and E2E tests. If implementation threatens the 400-line review budget, `ask-on-risk` should pause for a delivery decision rather than silently expanding into an oversized PR.
- **Access misconception:** Campaign IDs are not authorization. This local slice must not imply Internet exposure is safe.

## Rollback

- Revert the Block 01 application changes and database migration if no production data exists.
- If campaign rows were created in a local development database, drop the new campaign table/indexes or reset the local PostgreSQL volume according to the project development workflow.
- Because no ingestion, source data, AI state, deletion, accounts, or external provider integration is introduced, rollback is limited to campaign metadata and route/API shell behavior.

## Success criteria

- `/campaigns` lists existing campaigns and shows a creation-focused empty state when none exist.
- Creating a valid campaign persists trimmed `name`, generated opaque `id`, `createdAt`, and `updatedAt`.
- Invalid names and case-insensitive duplicates return field-level validation/conflict feedback without creating a campaign.
- `/campaigns/{campaignId}/chat` renders a campaign-scoped Chat shell with no AI query, ingestion, or citation behavior.
- `/campaigns/{campaignId}/sources` renders a campaign-scoped Sources shell with no upload, paste, import, update, removal, or indexing behavior.
- Unknown or malformed campaign routes show validation/not-found states and never fall back to another campaign.
- Backend tests prove campaign invariants, endpoint success/error cases, unknown lookup, malformed ID handling, and database-backed duplicate protection.
- Frontend tests prove `/campaigns`, Chat shell, Sources shell, loading, empty, validation-error, not-found, and success states.
- One focused E2E smoke test proves creating a campaign, selecting it, and navigating to Chat and Sources shells.
- No accounts, teams, deletion, ingestion, source lifecycle, AI, retrieval, or citations are exposed by this slice.

## Proposal question round

No additional product interview is required in this proposal phase because the orchestrator provided confirmed Block 01 scope and planning points. The following assumptions should be reviewed in later design/spec phases only if implementation detail creates risk:

1. Campaign identifiers remain opaque publicly, while the design phase chooses the concrete generated format and malformed-route validation behavior.
2. The database uniqueness strategy is an implementation design decision as long as PostgreSQL enforces case-insensitive campaign-name uniqueness.
3. Block 01 UI copy should be visible in English and describe unavailable future capabilities without adding disabled or placeholder actions for ingestion or AI.
4. A handwritten Angular API adapter is acceptable for Block 01 if it does not undermine the Block 02 generated-client contract workflow.

## References

- `PRODUCT.md`
- `DESIGN.md`
- `docs/02-requirements-and-acceptance.md`
- `docs/03-domain-model.md`
- `docs/06-api-contracts.md`
- `docs/10-roadmap.md`
- `docs/12-workspace-and-ingestion-ux.md`
- `openspec/changes/block-01-campaign-workspace-slice/explore.md`
