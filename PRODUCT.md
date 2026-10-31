# Lorekeeper — Product Source of Truth

Lorekeeper is an AI-assisted campaign memory companion for Game Masters, initially a personal PoC for Dani and his campaigns. It recovers relevant notes and answers with sources as a Lazy Lands experiment.

## Product principles

- Support multiple campaigns within one personal installation.
- The operator selects a campaign before ingestion or querying; evidence never crosses campaign boundaries.
- The GM controls canon. Preparation, events that happened and player knowledge are distinct; presence in a note does not prove players discovered it.
- Missing evidence produces an explicit limitation rather than invented facts.

## Core flow

Select a campaign → ingest its configured notes → inspect ingestion outcome → ask within that campaign → review an answer and its sources.

The first implementation slice is narrower: create or select a campaign and enter an empty campaign workspace with Chat and Sources routes. It does not include ingestion, AI chat, retrieval, citations or campaign deletion.

The first useful version follows: import Markdown/text notes and pasted text into a campaign, then search them and open the matching source fragments, without AI-generated answers. Grounded answers in Chat build on that evidence.

Exact screen design, API schemas, knowledge-status representation and generation acceptance criteria remain subject to their roadmap entry gates. This flow does not authorize additional features.

## Boundaries

Personal use, multiple campaigns, no multiuser product in the initial release. Application login is deferred; access control must be resolved before Internet exposure. No accounts, teams, invitations, cross-campaign search or automatic canon changes.

See [scope](docs/01-scope.md), [requirements](docs/02-requirements-and-acceptance.md) and [roadmap](docs/10-roadmap.md).

## Initial workspace approach

The selected campaign contains Chat and Sources, with campaign selection/creation accessible from navigation. Sources owns ingestion, progress, updates and removal; Chat consumes available evidence and opens citations. First ingestion supports multiple Markdown/text files and pasted text, followed by Notion. See [workspace and ingestion UX](docs/12-workspace-and-ingestion-ux.md) for the accepted first approach and unresolved slice decisions.
