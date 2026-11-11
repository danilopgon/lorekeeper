# 12 — Workspace and Ingestion UX: First Approach

**Status:** Accepted initial approach; 04A specified on 2026-11-11; later slices pending.  
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
3. Review detected documents: title, origin/format, optional content category, knowledge status and destination campaign. The browser reads the files, validates extension, size and UTF-8, proposes a title from the first heading or the file name, and computes each content hash; the server only reports which hashes already exist in the campaign. Invalid files are shown and not sent. Duplicate content is flagged and excluded by default. The server validates everything again on import (`06`).
4. Confirm import. Bind the operation to that campaign even if the user later navigates elsewhere.
5. Show progress per document, then availability or an actionable error and retry.

Content categories: session notes, preparation, characters/places, general reference; optional/unclassified is valid. Category is not knowledge status: session notes do not prove an event occurred or that players discovered a fact. Unknown knowledge status must remain possible.

## Progress and source list

User-facing stages show only steps that exist: **En cola → Procesando → Disponible**, with **Error** and Reintentar (decided 2026-11-11; this replaces the earlier Reading → Preparing → Indexing → Available proposal). 05A adds an indexing stage only if its lexical index is not built in the same transaction as the chunks; 05B adds an embedding stage. Jobs follow ADR-009 and progress streams over SSE (ADR-010). Partial batch failure must preserve successful documents and identify failed ones. Expose useful errors without provider credentials or private internal payloads.

Rows show title, origin, state, last successful update and available actions: inspect, update, edit metadata (title, category, knowledge status), remove; retry after failure. The inspect action arrives with 05A, together with fragment and locator display. Show pending/failed updates distinctly from the availability of an older indexed version. Use accessible status announcements, keyboard navigation and loading/empty/error/success states. Colour alone does not communicate status.

## Update and removal semantics

- Updating an existing source replaces its indexed version rather than creating a duplicate source. A normal new import is not automatically an update solely because its filename matches.
- Keep the previous usable indexed version while preparing a replacement. Publish a complete successful replacement atomically for retrieval; a failed update leaves the old version usable.
- An update is started explicitly from the source's row. Content identical to the published version changes nothing. After a successful publication the superseded version is deleted; no history is kept.
- Removing a source excludes it from future retrieval and answer generation, and wins over any job still processing it. Its content is deleted and a tombstone remains (`03`). Behaviour against an in-flight query is decided with 05A.
- Existing conversation citations indicate when a source is no longer available; do not silently resolve them to unrelated content. Historical citation resolution after an update is a pre-06 decision.

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

Before **04A**: recorded on 2026-11-11 in `02` (US-02), `03` (Source), `06` (Sources), `07` and ADR-008 to ADR-011. The inspect action moved to 05A. Readiness is tracked in `10`.

Before **05A** also: what inspecting a source shows (text, fragments, locators) and its contract.

Before **05A**: where search lives (Sources, Chat or its own route), result fragment and locator display, empty/no-match states and how results link to the source fragment.

Before **04B**: Notion credential/root configuration, traversal/preview, supported blocks, synchronisation and remote deletion semantics. No OAuth/multiuser subsystem is implied.

Before **06**: conversation persistence/reset policy, draft handling, response transport, source-fragment display and historical citation/version resolution. Final visual direction belongs in DESIGN.md.

## Roadmap mapping

**01:** campaign creation/selection and workspace navigation.  
**04A:** Sources with Markdown/text upload and pasted text.  
**05A:** campaign-scoped lexical search over available sources, showing fragments and real locators without AI answers.  
**04B:** Notion adapter and source update integration.  
**06:** Chat, grounded answers and citations.  
**08:** complete-flow verification and deployment access gate.

Plan both sections and their states early; implement behaviour only in its ready block. This first approach does not mark any implementation block ready or complete.

## Adopted visual system

[DESIGN.md](../DESIGN.md) adopts Codex Lithographica. Canonical-fact and table-knowledge blocks are conditional on explicit evidence, not mandatory answer sections. Citation lines/folios must be real traceable locators; otherwise use the source title, heading and fragment. Existing workspace, responsive, accessible interaction and lifecycle rules remain applicable.
