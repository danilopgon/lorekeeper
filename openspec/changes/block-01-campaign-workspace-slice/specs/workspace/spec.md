# Workspace Specification

## Purpose

The workspace provides URL-scoped campaign selection and empty Chat and Sources shells for the first Lorekeeper slice, while making unavailable future capabilities explicit and non-interactive. Visible application UI copy for this slice is English.

## Requirements

### Requirement: Campaign selection starts at `/campaigns` without implicit workspace entry

The system MUST provide a `/campaigns` route that lists available campaigns and supports campaign creation.
When no campaigns exist, `/campaigns` MUST show an empty state with a campaign creation action.
The system MUST NOT silently enter Chat or Sources from `/campaigns` without an explicit campaign workspace route.

#### Scenario: No campaigns state supports creation only

- GIVEN no campaigns exist
- WHEN the operator opens `/campaigns`
- THEN the page shows an empty campaign-selection state
- AND the page provides a campaign creation action
- AND the page does not enter a Chat or Sources workspace

#### Scenario: Existing campaigns can be selected explicitly

- GIVEN one or more campaigns exist
- WHEN the operator opens `/campaigns`
- THEN the page lists available campaigns
- AND selecting a campaign provides explicit navigation to that campaign's Chat or Sources route
- AND the selection does not create a hidden server or browser default that overrides the URL

### Requirement: Campaign creation UI exposes explicit request states

The campaign creation UI MUST expose loading, validation-error, recoverable API-error, success, and empty/list states as applicable.
Validation and conflict feedback MUST be associated with the campaign name field when the failure concerns the submitted name.
Recoverable failures MUST allow the operator to remain on the selection flow and retry without losing the explicit campaign boundary.

#### Scenario: Creation shows loading and success states

- GIVEN the operator submits a valid unique campaign name
- WHEN the creation request is pending
- THEN the UI shows a loading state for the creation action
- WHEN creation succeeds
- THEN the new campaign appears as selectable
- AND navigation to `/campaigns/{campaignId}/chat` or `/campaigns/{campaignId}/sources` is available for that campaign

#### Scenario: Invalid name shows field-level feedback

- GIVEN the operator submits a missing, whitespace-only, or overlong campaign name
- WHEN the API reports `campaign_name_invalid`
- THEN the UI shows a validation error associated with the campaign name field
- AND no campaign workspace is entered

#### Scenario: Duplicate name shows field-level conflict feedback

- GIVEN the operator submits a campaign name that duplicates an existing name case-insensitively
- WHEN the API reports `campaign_name_conflict`
- THEN the UI shows recoverable name-conflict feedback associated with the campaign name field
- AND no duplicate campaign appears in the list

#### Scenario: Recoverable API error stays in selection flow

- GIVEN the campaign list or creation request fails because of a recoverable API error
- WHEN the error is shown
- THEN the UI communicates the failure without entering a workspace
- AND the operator can retry the relevant action

### Requirement: Active campaign is represented by the URL

The system MUST represent the active campaign only through URL-scoped routes such as `/campaigns/{campaignId}/chat` and `/campaigns/{campaignId}/sources`.
A stored preference, session state, or previous selection MUST NOT override the campaign identifier in the URL.
Malformed or unknown campaign routes MUST show validation or not-found states without falling back to another campaign.

#### Scenario: URL determines the Chat campaign

- GIVEN the operator opens `/campaigns/{campaignId}/chat` for an existing campaign
- WHEN the route resolves
- THEN the Chat shell is scoped to the campaign identified by the URL
- AND no previous campaign selection overrides that route

#### Scenario: URL determines the Sources campaign

- GIVEN the operator opens `/campaigns/{campaignId}/sources` for an existing campaign
- WHEN the route resolves
- THEN the Sources shell is scoped to the campaign identified by the URL
- AND no previous campaign selection overrides that route

#### Scenario: Malformed route does not fallback

- GIVEN the operator opens a workspace route with a malformed `campaignId`
- WHEN the route is resolved
- THEN the UI shows a validation state corresponding to `campaign_id_invalid`
- AND no other campaign workspace is rendered

#### Scenario: Unknown route does not fallback

- GIVEN the operator opens a workspace route with a well-formed but unknown `campaignId`
- WHEN the route is resolved
- THEN the UI shows a not-found state corresponding to `campaign_not_found`
- AND no other campaign workspace is rendered

### Requirement: Chat shell communicates unavailable first-slice capabilities

The Chat workspace shell MUST render for a selected campaign and clearly communicate that AI chat, source-backed querying, retrieval, citations, conversation history, streaming, and draft persistence are not available in this slice.
The Chat shell MUST NOT expose usable or disabled-looking controls for AI queries, ingestion, retrieval, citations, source management, or future chat behavior.

#### Scenario: Existing campaign renders empty Chat shell

- GIVEN an existing campaign is selected by `/campaigns/{campaignId}/chat`
- WHEN the campaign resolves successfully
- THEN the Chat shell renders with that campaign context
- AND the shell states that AI/source-backed chat is not available in this slice
- AND the shell exposes no query submission, ingestion, retrieval, citation, or source-management action

#### Scenario: Chat shell has explicit route loading state

- GIVEN the operator opens a Chat workspace route
- WHEN campaign resolution is pending
- THEN the UI shows a loading state for the route
- AND it does not show another campaign's Chat content while loading

### Requirement: Sources shell communicates unavailable first-slice capabilities

The Sources workspace shell MUST render for a selected campaign and clearly communicate that ingestion and source management are not available in this slice.
The Sources shell MUST NOT expose upload, paste, Notion import, update, removal, indexing, retry, progress, or source lifecycle actions in this slice.

#### Scenario: Existing campaign renders empty Sources shell

- GIVEN an existing campaign is selected by `/campaigns/{campaignId}/sources`
- WHEN the campaign resolves successfully
- THEN the Sources shell renders with that campaign context
- AND the shell states that ingestion/source management is not available in this slice
- AND the shell exposes no upload, paste, import, update, removal, indexing, retry, or progress action

#### Scenario: Sources shell has explicit route loading state

- GIVEN the operator opens a Sources workspace route
- WHEN campaign resolution is pending
- THEN the UI shows a loading state for the route
- AND it does not show another campaign's Sources content while loading

### Requirement: Workspace UI follows accessible English Codex Lithographica behavior

The workspace UI MUST use English visible copy and the adopted Codex Lithographica direction without adding behavior outside the first slice.
Interactive controls MUST have accessible names, visible focus, keyboard operation, and errors associated with their fields or relevant status regions.
State must be communicated with text and structure, not color alone.
Responsive layout MUST keep campaign selection and Chat/Sources navigation accessible on small screens.

#### Scenario: Keyboard user creates and selects a campaign

- GIVEN the operator uses only a keyboard
- WHEN they open `/campaigns`, create a valid campaign, and navigate to Chat or Sources
- THEN all required controls are reachable and operable by keyboard
- AND focus remains visible throughout the flow

#### Scenario: Error state is accessible without color

- GIVEN the campaign creation request fails validation or conflict handling
- WHEN the error is displayed
- THEN the error is conveyed through English text associated with the affected field
- AND color is not the only indicator of the error state

#### Scenario: Small-screen navigation preserves campaign context

- GIVEN the workspace is viewed on a small screen
- WHEN the operator navigates between campaign selection, Chat, and Sources routes
- THEN the campaign selector or navigation remains accessible
- AND the active campaign remains determined by the URL
