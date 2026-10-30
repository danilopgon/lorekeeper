# 11 — Backlog and Non-goals

This document protects the current scope from attractive distractions.

## Deferred opportunities

| Idea | Why it may matter | Revisit when | Constraints |
| --- | --- | --- | --- |
| Codex Lithographica craft pass (#17): dithered imagery, restrained texture, optical refinement | Gives the product its intended visual finish | After the DS slices (#8) deliver fonts, tokens and primitives; maintainer-led | Decoration stays out of the accessibility tree and never obscures content or captures pointer input; ship exported assets, no runtime image tooling |
| Future product components: `OracleVerdictCard`, `IngestionDropzone`, `SourceStatusRow`, `MarginaliaColumn`, session indicators, indexed-source counters, campaign filters, campaign artwork, archive actions | Patterns already sketched in design references | The block that owns the capability (04A Sources, 05A search, 06 Chat) | Do not render them as disabled placeholders before their capability exists |

Deferred means **not part of the active roadmap**. Do not scaffold it. Optional engineering spikes (#18–#22) are positioned in the roadmap's non-blocking evaluations.

## Rejected ideas

Rejected architecture links to an ADR. Rejected product ideas stay here with a concise rationale.


## Explicit initial non-goals

Accounts, teams, invitations, shared roles/permissions, cross-campaign retrieval and automatic canon modification are outside the personal PoC. Multiuser support requires a fresh scope and ownership/authentication ADR. Application login is deferred pending deployment access design; control of remote access itself is mandatory before exposure.

## Initial workspace/ingestion approach

PDF, DOCX and ZIP import are deferred beyond the first ingestion approach. Revisit when real sources require them; define extraction quality and format limits before implementation. Initial formats are Markdown, plain text and pasted text, then Notion.
