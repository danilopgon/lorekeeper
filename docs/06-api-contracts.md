# 06 — API Contracts

## Conventions

- Base path: `/api`.
- Authentication: none for local development in the first slice. A campaign identifier is not an authentication credential. Internet exposure remains blocked until `07-data-security-and-rls.md` selects and verifies an access-control layer.
- Error format: RFC 7807 Problem Details with a stable `code` extension for expected client-handled failures.
- Dates and times: UTC ISO 8601 strings.
- Pagination/versioning/idempotency: no pagination, public API versioning or idempotency key is required for the first campaign slice. Add those only when a later slice needs them.

## Source of truth and client generation

ASP.NET endpoint contracts generate OpenAPI. Block 01 uses a narrow handwritten Angular campaign adapter behind the feature boundary; Block 02 replaces it with generated contract artefacts and a contract-diff gate. Decisions recorded 2026-10-31:

| Concern | Decision |
| --- | --- |
| Document generation | `Microsoft.AspNetCore.OpenApi` (.NET 10 built-in) describes the API. `Microsoft.Extensions.ApiDescription.Server` writes the document at build time, so generation must not need a running database or secrets. |
| Document location | `services/api/openapi/v1.json`, committed. |
| Error schema | 4xx responses declare `ProblemDetails` with the `code` extension used by the frontend error mapping. |
| Runtime exposure | `/openapi/v1.json` and Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`, reading that document) are mapped only when the environment is `Development`. No other environment serves either. |
| TypeScript generator | `@hey-api/openapi-ts` with the `@hey-api/typescript` and `zod` plugins only: types and Zod schemas, no SDK, client or framework plugin. Pinned to an exact version while it is 0.x. |
| Generated output | `apps/web/src/app/shared/api/generated/`, committed. |
| Frontend use | Queries and commands consume the generated types and schemas as defined in [ADR-006](decisions/ADR-006-frontend-cqrs-data-access.md). |
| Contract-diff policy | CI rebuilds the API, regenerates the document and the TypeScript output, and fails if either differs from the committed files. Contract changes are therefore reviewed as diffs in the PR. |

- Never hand-edit generated files.
- Automated breaking-change detection (for example `oasdiff`) is not required while the API has a single same-origin consumer; add it with a public API version.
- Breaking changes require an explicit migration or versioning decision.

## Campaign DTOs

### `CampaignDto`

```json
{
  "id": "opaque-id",
  "name": "Ash Crown",
  "createdAt": "2026-09-15T12:00:00Z",
  "updatedAt": "2026-09-15T12:00:00Z"
}
```

### `CreateCampaignRequest`

```json
{
  "name": "Ash Crown"
}
```

Rules:

- `name` is required.
- The server trims `name` before validation and persistence.
- Trimmed `name` length is 1–120 characters.
- Trimmed `name` is unique case-insensitively across campaigns.
- Clients must treat `id`, `createdAt` and `updatedAt` as server-owned. Block 01 IDs are lowercase canonical UUID strings, while consumers must continue to treat them as opaque selectors.

## `GET /api/campaigns`

**Purpose:** List campaigns available in the personal installation for `/campaigns` selection.  
**Authorization:** Local personal installation only in the first slice; no account/tenant rule exists yet.

### Request

No body.

### Success response

```json
[
  {
    "id": "opaque-id",
    "name": "Ash Crown",
    "createdAt": "2026-09-15T12:00:00Z",
    "updatedAt": "2026-09-15T12:00:00Z"
  }
]
```

### Expected errors

| Status | Condition | Public code |
| ---: | --- | --- |
| 500 | Unexpected server failure | `unexpected_error` |

## `POST /api/campaigns`

**Purpose:** Create a campaign for the first workspace slice.  
**Authorization:** Local personal installation only in the first slice; no account/tenant rule exists yet.

### Request

```json
{
  "name": "Ash Crown"
}
```

### Success response

Status: `201 Created`

```json
{
  "id": "opaque-id",
  "name": "Ash Crown",
  "createdAt": "2026-09-15T12:00:00Z",
  "updatedAt": "2026-09-15T12:00:00Z"
}
```

### Expected errors

| Status | Condition | Public code |
| ---: | --- | --- |
| 400 | `name` is missing, empty after trimming or longer than 120 characters | `campaign_name_invalid` |
| 409 | Trimmed `name` duplicates an existing campaign name case-insensitively | `campaign_name_conflict` |
| 500 | Unexpected server failure | `unexpected_error` |

## `GET /api/campaigns/{campaignId}`

**Purpose:** Resolve the campaign named in `/campaigns/{campaignId}/chat` or `/campaigns/{campaignId}/sources`.  
**Authorization:** Local personal installation only in the first slice; no account/tenant rule exists yet.

### Request

No body.

### Success response

```json
{
  "id": "opaque-id",
  "name": "Ash Crown",
  "createdAt": "2026-09-15T12:00:00Z",
  "updatedAt": "2026-09-15T12:00:00Z"
}
```

### Expected errors

| Status | Condition | Public code |
| ---: | --- | --- |
| 400 | `campaignId` is malformed for the chosen identifier format | `campaign_id_invalid` |
| 404 | No campaign exists for `campaignId` | `campaign_not_found` |
| 500 | Unexpected server failure | `unexpected_error` |

## Explicitly absent from the first slice

- No `DELETE /api/campaigns/{campaignId}` endpoint.
- No source, ingestion, upload, pasted text, Notion, indexing, retrieval, chat or citation endpoints.
- No implicit active-campaign endpoint; the active campaign is represented by the URL.

## External integrations

Document provider contract, timeout, retry, mapping and fallback. Provider responses are not domain models. No external provider integration is used by the first campaign/workspace slice.

## Campaign contract requirements

Campaign-dependent requests carry an explicit `campaignId` in the agreed route or request contract. No implicit campaign fallback. Validate that nested source/document IDs belong to that campaign for reads and writes. A campaign identifier is not an authentication credential.

The first slice's campaign endpoint/DTO/error contracts are specified above for block 01. Block 02 automates generation; it does not postpone contract design. Decide synchronous versus job-based ingestion before block 04 and full-response versus streaming contracts before block 06.

## Initial workspace/ingestion approach

Before 04A, specify import review/confirmation, upload/pasted-text validation, per-document progress/errors/retry, source update and removal contracts. Decide job polling/event transport explicitly. Before 06, specify conversation scope and source-fragment/version resolution. The [UX approach](12-workspace-and-ingestion-ux.md) defines behaviour, not final endpoint schemas.
