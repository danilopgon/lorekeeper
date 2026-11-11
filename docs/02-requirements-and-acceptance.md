# 02 — Requirements and Acceptance

## Story index

| ID | Story | Priority | Status |
| --- | --- | --- | --- |
| US-01 | Create/select a campaign and enter an empty workspace shell | Must | Defined |
| US-02 | Import, update and remove Markdown/text sources in a campaign | Must | Defined (04A) |

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

## US-02 — Markdown/text sources

**As a** personal Game Master operator  
**I want** to import my Markdown and text notes into a campaign, keep them up to date and remove them  
**So that** later search and answers work over my own notes, scoped to that campaign.

Rules live in `03` (Source), contracts in `06` (Sources) and flow in `12`. Decided on 2026-11-11.

### Acceptance criteria

1. Given a campaign, when the operator selects `.md`, `.markdown` or `.txt` files, then the review lists each file with a proposed title (first heading, otherwise file name), format, optional category, knowledge status (default `unknown`) and destination campaign before anything is imported.
2. Given the review, when a file has an unsupported extension, is larger than 1 MiB, is not valid UTF-8 or is empty, then it is marked invalid with a Spanish reason and is not sent.
3. Given the review, when a file's content matches a non-removed source in the same campaign, then it is flagged as a duplicate naming that source and excluded by default; the operator may include it anyway. Matching content in another campaign is never flagged.
4. Given more than 50 selected files, then the review refuses the selection and says the limit.
5. Given the operator confirms, when the import is accepted, then each document appears in the source list as `En cola`, then `Procesando`, then `Disponible` or `Error`, without reloading the page.
6. Given a batch where some documents fail, then the successful ones become available, the failed ones show their error and a Reintentar action, and retrying does not duplicate the successful ones.
7. Given pasted text with a title, then it follows the same flow; an empty title, empty text or text over 1 MiB is rejected with a field error.
8. Given an available source, when the operator updates it with new content, then the previous version stays searchable until the new one is available, and the source list shows the pending update separately from the available version.
9. Given an update whose processing fails, then the previous version stays available and the failure is shown with Reintentar.
10. Given an update with content identical to the published version, then nothing is reprocessed and the operator is told nothing changed.
11. Given a source, when the operator edits its title, category or knowledge status, then the change is saved without reprocessing.
12. Given a source, when the operator removes it, including while it is processing, then it disappears from the list and is never published or retrieved afterwards.
13. Given an import confirmed in campaign A, when the operator navigates to campaign B before it finishes, then B never shows A's sources or events, and the import still completes in A.
14. Given a lost event-stream connection, when it reconnects, then the list reloads and shows the current state.
15. Loading, empty, error and success states are explicit; status is conveyed by text, not colour alone; progress changes are announced to assistive technology; every action is keyboard operable.

### Not included

- Notion import (04B), PDF, DOCX and ZIP.
- Search and retrieval (05A), embeddings (05B), chat and citations (06).
- Source version history; only the published version is kept.
- The inspect action from `12`, moved to 05A.

### Verification

- Domain unit tests: title, size, encoding, hash and chunking rules, including headings, long sections, code blocks, tables and frontmatter.
- Validator unit tests for request shape (ADR-011).
- Integration tests against PostgreSQL: atomic version and job creation, publication, failed-update preservation, identical-content update, removal during processing, retry without duplicates, lease recovery and campaign isolation with overlapping titles.
- Contract tests through the generated OpenAPI document.
- Component tests for review, list states and SSE reconnection.
- Playwright: import a batch with one invalid file, see both outcomes, update a source and remove it.

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
