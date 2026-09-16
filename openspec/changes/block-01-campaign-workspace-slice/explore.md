# Explore — Block 01 Campaign Workspace Slice

## Status

Ready for proposal with documented readiness checks.

## Change name

`block-01-campaign-workspace-slice`

## Problem

Lorekeeper has the scaffold and CI in place, but the first product flow is not implemented. The operator needs an explicit campaign context before future ingestion or AI features can exist. Campaign selection must be URL-scoped and must not rely on hidden defaults.

## Scope

Implement the Block 01 slice only:

- campaign creation and selection;
- persistence in PostgreSQL via the .NET API;
- campaign routes in Angular:
  - `/campaigns`
  - `/campaigns/{campaignId}/chat`
  - `/campaigns/{campaignId}/sources`;
- empty Chat and Sources workspace shells that communicate unavailable future capabilities;
- explicit loading, empty, validation-error, not-found, and success states;
- tests proving campaign invariants, API behavior, frontend route states, and one focused E2E smoke flow.

## Non-goals

Do not implement:

- campaign deletion, archival, soft delete, or cascade policy;
- ingestion, uploads, pasted text, Notion, indexing, retrieval, citations, source lifecycle, or AI chat;
- accounts, teams, invitations, multiuser permissions, or Internet-exposure access control;
- generated OpenAPI client automation beyond what Block 01 needs directly, because Block 02 owns generated client workflow.

## Existing specification evidence

- `PRODUCT.md` defines the first implementation slice as campaign creation/selection plus empty Chat and Sources workspace routes.
- `docs/02-requirements-and-acceptance.md` defines US-01 acceptance criteria and verification expectations.
- `docs/03-domain-model.md` defines Campaign fields and invariants.
- `docs/06-api-contracts.md` defines `GET /api/campaigns`, `POST /api/campaigns`, and `GET /api/campaigns/{campaignId}` with expected errors.
- `docs/10-roadmap.md` marks Block 00 Done and Block 01 Not started pending readiness confirmation against `02`, `03`, `06`, and `DESIGN.md`.
- `docs/12-workspace-and-ingestion-ux.md` maps campaign creation/selection and workspace navigation to Block 01.
- `DESIGN.md` supplies the Codex Lithographica UI direction and state/accessibility expectations.

## Readiness notes

Resolved enough for proposal:

- Campaign fields: `id`, `name`, `createdAt`, `updatedAt`.
- Name validation: trim, required, 1–120 characters, case-insensitive uniqueness.
- URL campaign selection is explicit; no implicit active campaign endpoint.
- API contracts and public error codes are specified for Block 01.
- First-slice non-goals are explicit.

Needs proposal/design attention before apply:

- Choose concrete Campaign ID format and malformed-ID behavior in implementation.
- Confirm EF Core schema/index strategy for case-insensitive uniqueness in PostgreSQL.
- Define minimal wording for unavailable Chat/Sources shell states using the design system.
- Decide whether Angular calls handwritten API adapters for Block 01 or waits for generated client in Block 02.

## Recommended next phase

Create the SDD proposal for `block-01-campaign-workspace-slice`, then produce spec, design, tasks, apply, verify, sync, and archive through the SDD lifecycle.
