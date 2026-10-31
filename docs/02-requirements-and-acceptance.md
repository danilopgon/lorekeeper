# 02 — Requirements and Acceptance

## Story index

| ID | Story | Priority | Status |
| --- | --- | --- | --- |
| US-01 | Create/select a campaign and enter an empty workspace shell | Must | Defined |

## US-01 — Campaign selection and empty workspace shell

**As a** personal Game Master operator  
**I want** to create or select a campaign and open its Chat or Sources workspace  
**So that** all later notes and questions have an explicit campaign context before ingestion or AI is introduced.

### Acceptance criteria

1. Given no campaigns exist, when the operator opens `/campaigns`, then the page shows an empty state with a campaign creation action and no Chat or Sources workspace is entered without a campaign.
2. Given the operator creates a campaign with a valid name, when creation succeeds, then the campaign is persisted with `id`, trimmed `name`, `createdAt` and `updatedAt`, and the operator can navigate to `/campaigns/{campaignId}/chat` or `/campaigns/{campaignId}/sources`.
3. Given the operator submits a campaign name, when it is empty after trimming, longer than 120 characters, or duplicates an existing campaign name case-insensitively, then creation is rejected with a field-level validation error and no campaign is created.
4. Given one or more campaigns exist, when the operator opens `/campaigns`, then the page lists available campaigns and supports selecting one without defaulting future work to an implicit campaign.
5. Given a selected campaign, when the operator opens `/campaigns/{campaignId}/chat`, then the Chat workspace shell renders for that campaign, contains no ingestion or AI query capability, and communicates that sources/AI are not available in this slice.
6. Given a selected campaign, when the operator opens `/campaigns/{campaignId}/sources`, then the Sources workspace shell renders for that campaign, contains no upload, paste, import, update or removal actions, and communicates that ingestion is not available in this slice.
7. Given an unknown or malformed `campaignId` in `/campaigns/{campaignId}/chat` or `/campaigns/{campaignId}/sources`, when the route is requested, then the UI/API reports a not-found or validation state without falling back to another campaign.
8. Loading, empty, validation-error, not-found and success states are explicit for campaign creation, campaign selection and both workspace shell routes.
9. Given two concurrent requests create the same trimmed name with different casing, when they reach persistence, then database-backed uniqueness keeps one campaign and the other request fails with `campaign_name_conflict`.
10. Given a workspace route is loading or switching campaign, when resolution is pending, then no content from a previously shown campaign is rendered.
11. Given a keyboard-only operator, when they create a campaign and navigate to Chat or Sources, then every control is reachable and operable with visible focus, and errors are conveyed by text associated with the affected field, not by colour alone.

### Not included

- Campaign deletion.
- Source ingestion, upload, pasted text, Notion import, source update, source removal or indexing.
- AI chat, retrieval, grounded answers, citations, conversation history or draft persistence.
- Accounts, teams, invitations, multiuser permissions or Internet-exposure access control.

### Verification

- Domain/unit tests cover campaign name trimming, required/length validation and case-insensitive uniqueness.
- Backend integration tests cover create/list/get campaign endpoints, unknown campaign lookup and duplicate-name persistence protection.
- Frontend component or route tests cover `/campaigns`, `/campaigns/:campaignId/chat` and `/campaigns/:campaignId/sources` empty/loading/error/success states.
- A focused E2E smoke test covers creating a campaign, selecting it and navigating to Chat and Sources shells.
- No ingestion or AI commands/actions are exposed in this slice.

## Accepted baseline requirements

### LK-01 — Select a campaign

Given multiple campaigns, when the operator starts ingestion or a query, an explicit campaign must be selected. Missing, invalid or unknown campaign identifiers fail validation without defaulting to another campaign. Exact HTTP mapping is defined before the slice implementing it.

### LK-02 — Isolate evidence

Given two campaigns with overlapping NPC names, querying campaign A returns only A's chunks and citations. Campaign filtering applies to lexical and vector candidate retrieval before ranking. Reranking, context construction, caches and source resolution retain the same scope.

Verification: integration test with real PostgreSQL/pgvector and overlapping-name fixtures; assert no campaign B content or source identifiers appear.

### LK-03 — Isolate mutations

Ingesting, reindexing or deleting a source in campaign A cannot modify campaign B. A source/document reference from another campaign is rejected. Verify database constraints and application checks with integration tests.

### LK-04 — Personal access boundary

Local development needs no application login. Before Internet exposure, unauthorised access to both UI and API must be denied by the selected application or external access layer; direct-origin bypass must also be prevented and tested.

### Requirements before implementation

Each slice needs concrete input/output, validation, failure/recovery and relevant UI states plus verification evidence. Knowledge-status and source-backed generation acceptance criteria must be resolved at the roadmap gates before those behaviours are implemented.

## Initial workspace/ingestion approach

Expand the acceptance scenarios in [workspace and ingestion UX](12-workspace-and-ingestion-ux.md) into slice requirements before implementation: campaign switching and late responses, import validation/partial failures, update preservation, removal and historical citations. These supplement LK-01–04; unresolved contracts remain blocking for the affected slice.

## Design/evidence acceptance additions

Answers must not render canonical-fact or knowledge-asymmetry claims without supporting source evidence. Missing source mentions do not establish player ignorance. Citation locators must map to actual source locations, with title/heading/fragment fallback. Verify keyboard interaction, focus, readable responsive layout and real colour contrast for each implemented screen against DESIGN.md.
