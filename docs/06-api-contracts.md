# 06 — API Contracts

## Conventions

- Base path: `/api`.
- Authentication: none for local development in the first slice. A campaign identifier is not an authentication credential. Internet exposure remains blocked until `07-data-security-and-rls.md` selects and verifies an access-control layer.
- Error format: RFC 7807 Problem Details with a stable `code` extension for expected client-handled failures.
- Dates and times: UTC ISO 8601 strings.
- Pagination/versioning/idempotency: no pagination, public API versioning or idempotency key is required for the first campaign slice. Add those only when a later slice needs them.

## Source of truth and client generation

ASP.NET endpoint contracts generate OpenAPI. Block 01 uses a narrow handwritten Angular campaign adapter behind the feature boundary; Block 02 owns generated TypeScript client automation and its contract-diff workflow.

- Never hand-edit generated files when generation is introduced.
- Commit generated output only if the repository chooses that strategy consistently.
- Block 02 must make CI regeneration and unexpected-diff handling executable.
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
