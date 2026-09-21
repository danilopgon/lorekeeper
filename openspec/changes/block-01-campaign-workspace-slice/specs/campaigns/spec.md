# Campaigns Specification

## Purpose

Campaigns provide the explicit persisted boundary for Lorekeeper's first workspace slice so future sources, retrieval and chat cannot rely on hidden defaults or cross-campaign context.

## Requirements

### Requirement: Campaign records are persisted with server-owned identity and timestamps

The system MUST persist campaigns with a system-generated opaque `id`, trimmed `name`, server-owned `createdAt`, and server-owned `updatedAt`.
Clients MUST NOT provide authoritative values for `id`, `createdAt`, or `updatedAt`.
On creation, `updatedAt` MUST represent the campaign's creation-time metadata state.

#### Scenario: Create a campaign with a trimmed name

- GIVEN the operator submits a campaign name with leading or trailing whitespace
- WHEN the campaign is created successfully
- THEN the persisted campaign name is the trimmed value
- AND the response includes an opaque `id`, `createdAt`, and `updatedAt`
- AND the response does not use client-supplied identity or timestamp values

#### Scenario: List persisted campaign records

- GIVEN one or more campaigns have been created
- WHEN the operator requests the campaign list
- THEN the system returns each campaign with `id`, `name`, `createdAt`, and `updatedAt`
- AND each timestamp is represented as a UTC ISO 8601 value

### Requirement: Campaign names are validated before creation

The system MUST reject campaign creation when `name` is missing, empty after trimming, or longer than 120 characters after trimming.
The system MUST return an RFC 7807 Problem Details response with stable public code `campaign_name_invalid` for these validation failures.
The system MUST NOT create a campaign when validation fails.

#### Scenario: Missing campaign name is rejected

- GIVEN the operator submits a campaign creation request without a `name`
- WHEN the API validates the request
- THEN the response is a validation failure with code `campaign_name_invalid`
- AND no campaign is created

#### Scenario: Whitespace-only campaign name is rejected

- GIVEN the operator submits a campaign name containing only whitespace
- WHEN the API validates the request after trimming
- THEN the response is a validation failure with code `campaign_name_invalid`
- AND no campaign is created

#### Scenario: Overlong campaign name is rejected

- GIVEN the operator submits a campaign name longer than 120 characters after trimming
- WHEN the API validates the request
- THEN the response is a validation failure with code `campaign_name_invalid`
- AND no campaign is created

### Requirement: Campaign names are unique case-insensitively

The system MUST enforce case-insensitive uniqueness for trimmed campaign names across the personal installation.
The system MUST enforce this uniqueness at the database boundary as well as in application behavior.
Duplicate creation attempts MUST return an RFC 7807 Problem Details response with stable public code `campaign_name_conflict`.
The system MUST NOT create a duplicate campaign when a conflict occurs.

#### Scenario: Case-insensitive duplicate name is rejected

- GIVEN a campaign named `Ash Crown` exists
- WHEN the operator submits a new campaign named `ash crown`
- THEN campaign creation fails with code `campaign_name_conflict`
- AND only the original campaign exists

#### Scenario: Concurrent duplicate creation is protected

- GIVEN two requests attempt to create the same trimmed campaign name with different casing
- WHEN the requests reach persistence concurrently
- THEN database-backed uniqueness prevents duplicate persisted campaigns
- AND the losing request is reported as `campaign_name_conflict`

### Requirement: Campaign endpoints expose only first-slice campaign operations

The system MUST expose campaign listing, creation, and lookup under `/api` as `GET /api/campaigns`, `POST /api/campaigns`, and `GET /api/campaigns/{campaignId}`.
The system MUST NOT expose campaign deletion, implicit active-campaign, source, ingestion, chat, retrieval, citation, account, team, or authentication endpoints as part of this slice.

#### Scenario: List campaigns

- GIVEN no campaigns or multiple campaigns exist
- WHEN `GET /api/campaigns` is requested
- THEN the response represents the available campaigns in the personal installation
- AND the response does not select an implicit active campaign

#### Scenario: Create campaign returns created resource

- GIVEN the operator submits a valid unique campaign name
- WHEN `POST /api/campaigns` succeeds
- THEN the response status is `201 Created`
- AND the response body is the created campaign DTO

#### Scenario: Absent first-slice operations remain unavailable

- GIVEN the first campaign workspace slice is implemented
- WHEN a consumer looks for deletion, ingestion, source lifecycle, chat, retrieval, citation, account, team, or implicit active-campaign operations
- THEN those operations are not exposed by this slice

### Requirement: Campaign lookup validates malformed and unknown identifiers without fallback

The system MUST validate `campaignId` according to the chosen opaque identifier format.
Malformed identifiers MUST fail with an RFC 7807 Problem Details response using stable public code `campaign_id_invalid`.
Unknown well-formed identifiers MUST fail with an RFC 7807 Problem Details response using stable public code `campaign_not_found`.
The system MUST NOT resolve a malformed, unknown, or missing campaign identifier to another campaign or implicit default.

#### Scenario: Existing campaign is resolved by id

- GIVEN a campaign exists
- WHEN `GET /api/campaigns/{campaignId}` is requested with that campaign's identifier
- THEN the response contains that campaign's `id`, `name`, `createdAt`, and `updatedAt`

#### Scenario: Malformed campaign identifier is rejected

- GIVEN a campaign route contains an identifier that is malformed for the chosen identifier format
- WHEN the API resolves the identifier
- THEN the response fails with code `campaign_id_invalid`
- AND no other campaign is returned

#### Scenario: Unknown campaign identifier is not found

- GIVEN a well-formed campaign identifier does not match any persisted campaign
- WHEN the API resolves the identifier
- THEN the response fails with code `campaign_not_found`
- AND no other campaign is returned

### Requirement: Campaign identifiers are not authorization credentials

The system MUST treat campaign identifiers only as campaign selectors within the local personal installation.
The system MUST NOT represent campaign ID knowledge as authentication, authorization, multiuser permission, account membership, or Internet-exposure access control.

#### Scenario: Local campaign lookup does not imply access control

- GIVEN a campaign can be resolved by its identifier in local development
- WHEN first-slice API and UI behavior is presented
- THEN the behavior does not claim that the campaign identifier grants or proves authorization
- AND Internet exposure remains outside this slice
