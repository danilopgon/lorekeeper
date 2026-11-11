# 06 — API Contracts

## Conventions

- Base path: `/api`.
- Authentication: none for local development in the first slice. A campaign identifier is not an authentication credential. Internet exposure remains blocked until `07-data-security-and-rls.md` selects and verifies an access-control layer.
- Error format: RFC 7807 Problem Details with a stable `code` extension for expected client-handled failures.
- Dates and times: UTC ISO 8601 strings.
- Pagination/versioning/idempotency: no pagination, public API versioning or idempotency key is required for the first campaign slice. Add those only when a later slice needs them. Source import is not idempotent by key; the review step flags duplicate content instead (`03`).

## Source of truth and client generation

ASP.NET endpoint contracts generate OpenAPI. Block 01 uses a narrow handwritten Angular campaign adapter behind the feature boundary; Block 02 replaces it with generated contract artefacts and a contract-diff gate. Decisions recorded 2026-10-31:

| Concern | Decision |
| --- | --- |
| Document generation | `Microsoft.AspNetCore.OpenApi` (.NET 10 built-in) describes the API. `Microsoft.Extensions.ApiDescription.Server` writes the document at build time, so generation must not need a running database or secrets. |
| Document location | `services/api/openapi/v1.json`, committed. |
| Error schema | Campaign 4xx responses declare `application/problem+json` with `ProblemDetails`, which adds the optional `code` and `errors` extensions. They are optional because framework validation failures, such as malformed JSON, return a 400 without them; the frontend maps those to its recoverable state. |
| Runtime exposure | `/openapi/v1.json` and Swagger UI at `/swagger` (`Swashbuckle.AspNetCore.SwaggerUI`, reading that document) are mapped only when the environment is `Development`. No other environment serves either. |
| TypeScript generator | `@hey-api/openapi-ts` with the `@hey-api/typescript` and `zod` plugins only: types and Zod schemas, no SDK, client or framework plugin. Pinned to an exact version while it is 0.x. Date-time schemas accept numeric offsets because `DateTimeOffset` may serialize as `+00:00`. |
| Generated output | `apps/web/src/app/shared/api/generated/`, committed. |
| Frontend use | Queries and commands consume the generated types and schemas as defined in [ADR-006](decisions/ADR-006-frontend-cqrs-data-access.md). |
| Contract-diff policy | The `Contract` CI job runs `pnpm run contract:check`, which rebuilds the API, regenerates the document and the TypeScript output, and fails if either differs from the committed files. Contract changes are therefore reviewed as diffs in the PR. |

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

## Explicitly absent

- No `DELETE /api/campaigns/{campaignId}` endpoint.
- No Notion, retrieval, search, chat or citation endpoints until their blocks specify them.
- No implicit active-campaign endpoint; the active campaign is represented by the URL.

## Sources (block 04A)

Ingestion is job-based (ADR-009) and progress streams over SSE (ADR-010). Rules and invariants live in `03`. Every route below is under `/api/campaigns/{campaignId}/sources` and returns `campaign_id_invalid` (400) or `campaign_not_found` (404) like `GET /api/campaigns/{campaignId}`. A `sourceId` from another campaign is reported as `source_not_found`.

### Limits

| Limit | Value |
| --- | --- |
| Accepted files | `.md`, `.markdown`, `.txt`, UTF-8 with optional BOM |
| Text per file or paste | 1 MiB (1,048,576 bytes) once encoded as UTF-8 |
| Files per import request | 1–50 |
| Import request body | 52 MiB, set explicitly on that endpoint (above Kestrel's 30 MB default) |
| Title | 1–120 characters after trimming |
| Hashes per content-match request | 1–50 |

### `SourceDto`

```json
{
  "id": "opaque-id",
  "campaignId": "opaque-id",
  "title": "Sesión 3",
  "origin": "file",
  "category": "session_notes",
  "knowledgeStatus": "occurred",
  "availability": "available",
  "lastPublishedAt": "2026-11-11T10:00:00Z",
  "processing": {
    "state": "failed",
    "errorCode": "source_encoding_invalid",
    "attempts": 1
  },
  "createdAt": "2026-11-10T09:00:00Z",
  "updatedAt": "2026-11-11T10:05:00Z"
}
```

- `origin`: `file` or `paste`.
- `category`: `session_notes`, `preparation`, `characters_places`, `general_reference` or `null`.
- `knowledgeStatus`: `unknown`, `preparation` or `occurred`.
- `availability`: `available` when a version is published, otherwise `unavailable`.
- `lastPublishedAt`: when the published version became available, or `null`.
- `processing`: the pending version's job (`queued`, `processing` or `failed`), or `null` when nothing is pending. A source can be `available` and have a failed `processing` at once: the old version stays usable after a failed update.

### `GET /sources`

Lists non-removed sources, newest first. `200` with `SourceDto[]`.

### `POST /sources/content-matches`

Answers which content hashes already exist in this campaign, for the import review. The browser computes each hash exactly as `03` defines `contentHash`.

```json
{ "hashes": ["9f86d0…"] }
```

`200`:

```json
{ "matches": [{ "hash": "9f86d0…", "sourceId": "opaque-id", "title": "Sesión 3" }] }
```

Errors: `400 source_hashes_invalid` when the list is empty, longer than 50 or contains a value that is not 64 lowercase hex characters.

### `POST /sources/imports`

Imports a batch of files. `multipart/form-data` with:

- `files`: 1–50 file parts.
- `metadata`: one JSON part, an array with one entry per file in the same order: `{ "title": "…", "category": null, "knowledgeStatus": "unknown" }`.

The server validates request shape first (ADR-011), then each file independently. Duplicate content is not rejected; the review warns about it.

`202 Accepted`:

```json
{
  "items": [
    { "index": 0, "fileName": "sesion-3.md", "outcome": "accepted", "source": { "…": "SourceDto" }, "errorCode": null },
    { "index": 1, "fileName": "mapa.pdf", "outcome": "rejected", "source": null, "errorCode": "source_file_type_unsupported" }
  ]
}
```

Request errors (`400`, nothing imported): `source_batch_empty`, `source_batch_too_large`, `source_metadata_invalid` (missing, unparsable or count mismatch). Body above 52 MiB: `413`.

Per-item `errorCode`: `source_file_type_unsupported`, `source_too_large`, `source_encoding_invalid`, `source_empty`, `source_title_invalid`, `source_category_invalid`, `source_knowledge_status_invalid`.

### `POST /sources/pastes`

```json
{ "title": "Ideas para la sesión 4", "text": "…", "category": "preparation", "knowledgeStatus": "preparation" }
```

`202` with `SourceDto`. Errors (`400`): `source_title_invalid`, `source_empty`, `source_too_large`, `source_category_invalid`, `source_knowledge_status_invalid`.

### `PUT /sources/{sourceId}/content`

Explicit update. `multipart/form-data` with either a `file` part or a `text` field. The title is not changed.

- `202` with `SourceDto` when a new version is queued. A previously failed pending version is discarded.
- `200` with `SourceDto` when the content hash equals the published version's; nothing changes.

Errors: `400 source_id_invalid`, `404 source_not_found`, `409 source_processing` (a version is `queued` or `processing`), plus the content codes of `POST /sources/pastes` and `source_file_type_unsupported`.

### `PATCH /sources/{sourceId}`

Edits metadata without creating a version. Any subset of `title`, `category` (`null` clears it) and `knowledgeStatus`.

`200` with `SourceDto`. Errors: `400 source_id_invalid`, `source_title_invalid`, `source_category_invalid`, `source_knowledge_status_invalid`; `404 source_not_found`.

### `POST /sources/{sourceId}/retry`

Manually retries the failed pending version. `202` with `SourceDto`. Errors: `400 source_id_invalid`, `404 source_not_found`, `409 source_retry_unavailable` (nothing has failed).

### `DELETE /sources/{sourceId}`

Removes the source (`03`: removal wins over processing). `204`. Errors: `400 source_id_invalid`, `404 source_not_found` (including already removed).

### `GET /sources/events`

`text/event-stream` for one campaign. The client loads `GET /sources` first and again after every reconnection; events are not replayed.

| Event | Data |
| --- | --- |
| `source-changed` | `SourceDto` |
| `source-removed` | `{ "id": "opaque-id", "campaignId": "opaque-id" }` |

A comment line (`: ping`) is sent every 15 seconds to keep idle connections open.

### Inspect

`12` lists an inspect action for each source. Its contract is not specified yet; see the 04A readiness record in `10`.

## External integrations

Document provider contract, timeout, retry, mapping and fallback. Provider responses are not domain models. Block 04A calls no external provider. The AI provider policy for block 03 is in `05`.

## Campaign contract requirements

Campaign-dependent requests carry an explicit `campaignId` in the agreed route or request contract. No implicit campaign fallback. Validate that nested source/document IDs belong to that campaign for reads and writes. A campaign identifier is not an authentication credential.

Block 02 automates contract generation; it does not postpone contract design. Ingestion is job-based (ADR-009). Chat streams over SSE (ADR-010); its event schema is specified before block 06.

## Initial workspace/ingestion approach

The Sources contract above covers import review, validation, per-document progress, errors, retry, update and removal. Before 06, specify conversation scope and source-fragment/version resolution. The [UX approach](12-workspace-and-ingestion-ux.md) defines behaviour.
