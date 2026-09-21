# ADR-001 — Personal multicampaign PoC and SDD readiness

**Status:** Accepted  
**Date:** 2026-09-12

## Context

Lorekeeper initially serves Dani's campaigns. The technical baseline still contains templates; agents need explicit boundaries before implementing missing product behaviour.

## Decision

Support several isolated campaigns in a single-operator installation. Require explicit campaign identity through storage, ingestion and retrieval, without scaffolding multiuser entities. Campaign identity is not authorization.

Use per-block Definition of Ready and entry/exit evidence. Missing required decisions block the affected block, not all independent work. Create evaluation fixtures before retrieval and establish thresholds from measured results.

Defer the choice between application authentication and external private access until deployment design. Local-only work may proceed without login; Internet exposure is blocked until access to UI and API is enforced and verified, including direct-origin paths.

**Resolution:** [ADR-002](ADR-002-cloudflare-access-personal-deployment.md) later selects Cloudflare Access as that external private-access layer. The enforcement and verification gate remains unchanged.

## Alternatives and consequences

A single hardcoded campaign would undermine isolation testing; a multiuser product would add unrequested scope. The selected boundary enables personal experimentation while leaving future multiuser ownership as an explicit redesign decision.

## Verification

Cross-campaign integration tests, roadmap readiness records and deployment access smoke evidence. Revisit when sharing campaigns or adding users becomes an approved product goal.
