# 12 — Workspace and Ingestion UX: First Approach

**Status:** Accepted initial approach; detailed slice specifications pending.  
**Date:** 2026-09-13

This defines the initial product flow, not a final visual design or an executable API contract. Apply the roadmap Definition of Ready before implementing each slice.

## Workspace

A persistent campaign selector, with minimal campaign creation, scopes two sections: **Chat** and **Sources**. On smaller screens the selector remains accessible through navigation. Switching campaigns changes sources and conversation context; late responses from the previous campaign must never appear in the newly selected campaign. Conversation persistence, history and handling unsent drafts remain explicit pre-chat decisions.

### Block 01 implementation boundary

Block 01 delivers campaign creation/selection and URL-scoped Chat and Sources shells only. The shells resolve the campaign from `/campaigns/{campaignId}/chat` or `/campaigns/{campaignId}/sources`, show explicit loading, invalid-ID and not-found states, and state that their future capabilities are unavailable. They do not render query, upload, paste, import, source-management, retry or progress controls. The workflow below remains the accepted approach for Blocks 04A–06; it is not available in the Block 01 shells.

- **Chat:** ask questions, read source-backed answers and open the cited source fragment.
- **Sources:** add content, inspect processing state, update and remove sources.
- No campaigns: offer campaign creation.
- No available sources: show “Añade fuentes para consultar esta campaña” with an action to Sources.
- Some sources available: allow queries over those sources while showing that remaining imports are processing.

## Entry methods and sequence

| Entry | Delivery | Boundary |
| --- | --- | --- |
| Multiple .md/.txt files | 04A | Preserve Markdown headings for structural chunking |
| Pasted text | 04A | Reuse canonical-document processing; collect a source title |
| Notion import | 04B | Configure root per campaign; reuse the same downstream pipeline |
| PDF / DOCX | Deferred | Requires extraction and layout/table handling decisions |
| ZIP of Markdown | Deferred | Add only when bulk folder/export import is needed |

Do not render unfinished methods as working actions. Do not silently accept unsupported formats.

## Import flow

1. Open Sources → Add content within a selected campaign.
2. Choose an available entry method; select files, paste text or configure Notion.
3. Review detected documents: title, origin/format, optional content category and destination campaign. Display unsupported or invalid input before confirmation where possible.
4. Confirm import. Bind the operation to that campaign even if the user later navigates elsewhere.
5. Show progress per document, then availability or an actionable error and retry.

Content categories: session notes, preparation, characters/places, general reference; optional/unclassified is valid. Category is not knowledge status: session notes do not prove an event occurred or that players discovered a fact. Unknown knowledge status must remain possible.

## Progress and source list

User-facing stages: **Reading → Preparing → Indexing → Available**, with **Error** and retry. Internal job states and transport will be specified before implementation. Partial batch failure must preserve successful documents and identify failed ones. Expose useful errors without provider credentials or private internal payloads.

Rows show title, origin, state, last successful update and available actions: inspect, update, remove; retry after failure. Show pending/failed updates distinctly from the availability of an older indexed version. Use accessible status announcements, keyboard navigation and loading/empty/error/success states. Colour alone does not communicate status.

## Update and removal semantics

- Updating an existing source replaces its indexed version rather than creating a duplicate source. A normal new import is not automatically an update solely because its filename matches.
- Keep the previous usable indexed version while preparing a replacement. Publish a complete successful replacement atomically for retrieval; a failed update leaves the old version usable.
- Removing a source excludes it from future retrieval and answer generation. Define deletion-versus-in-flight-query behaviour before implementing removal.
- Existing conversation citations indicate when a source is no longer available; do not silently resolve them to unrelated content. Version/snapshot retention and historical citation resolution remain pre-implementation decisions.

## Acceptance scenarios to expand per slice

- Switching A → B never exposes A's conversation, source list or late query/import responses in B.
- Missing/unknown campaign prevents import and querying; active jobs retain the campaign selected at confirmation.
- Importing supported files/text produces individually visible results; unsupported files get explicit feedback.
- A partial batch failure permits retrying failed items without duplicating successful documents.
- Available sources can be queried while other imports continue; no-source chat links to ingestion.
- A failed replacement leaves the previous version retrievable; successful replacement does not combine old and new chunks.
- A removed source is excluded from subsequent retrieval; historical citations show unavailability when appropriate.

## Open decisions and gates

Before **01**: campaign creation fields, selector/navigation behaviour, minimal layout/tokens and screen states.

Before **04A**: upload size/count/encoding limits, pasted-text limits, supported Markdown handling, duplicate identity policy, preview API, job lifecycle, retry/idempotency, version publication, deletion races and retention. Define concrete DTOs and observable acceptance criteria in 02/03/06; update 05/07 for processing and data policies.

Before **04B**: Notion credential/root configuration, traversal/preview, supported blocks, synchronisation and remote deletion semantics. No OAuth/multiuser subsystem is implied.

Before **06**: conversation persistence/reset policy, draft handling, response transport, source-fragment display and historical citation/version resolution. Final visual direction belongs in DESIGN.md.

## Roadmap mapping

**01:** campaign creation/selection and workspace navigation.  
**04A:** Sources with Markdown/text upload and pasted text.  
**04B:** Notion adapter and source update integration.  
**06:** Chat, grounded answers and citations.  
**08:** complete-flow verification and deployment access gate.

Plan both sections and their states early; implement behaviour only in its ready block. This first approach does not mark any implementation block ready or complete.

## Adopted visual system

[DESIGN.md](../DESIGN.md) adopts Codex Lithographica. Canonical-fact and table-knowledge blocks are conditional on explicit evidence, not mandatory answer sections. Citation lines/folios must be real traceable locators; otherwise use the source title, heading and fragment. Existing workspace, responsive, accessible interaction and lifecycle rules remain applicable.
