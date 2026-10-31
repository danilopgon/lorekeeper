# ADR-005 — Spanish UI copy for the personal installation

**Status:** Accepted
**Date:** 2026-10-30
**Area:** frontend

## Context and problem

Lorekeeper is a personal, single-operator installation used by a Spanish-speaking GM. Block 01 shipped English visible copy, as recorded in its original design (now retired with OpenSpec; see git history up to `97099b6`). The maintainer approved Spanish UI copy in #8 on 2026-10-01. Migrating is cheapest before Sources, Search and Chat add more screens.

## Alternatives considered

### Option A — Keep English UI

- Benefits: no migration.
- Costs/risks: the only operator works in Spanish; every new screen would add copy to translate later.

### Option B — i18n framework with a language selector

- Benefits: supports several languages.
- Costs/risks: framework, extraction workflow and test overhead for one operator and one language.

### Option C — Spanish UI copy directly in templates

- Benefits: no framework; copy is reviewed where it is used.
- Costs/risks: adding another language later would require extraction.

## Decision

Use **Option C**: visible UI copy is Spanish, in a practical tabletop-GM register.

## Proposed design

- Labels, headings, actions, validation and conflict errors, loading/empty/error states, route states, page titles and accessible names are Spanish; `<html lang="es">`.
- The register is practical ("Crear campaña"), never ceremonial or liturgical. The monumental tone belongs to the visual system (`DESIGN.md`), not to the words.
- Code identifiers, API contracts, error codes, commits, ADRs and technical documentation stay English.
- API error codes are stable and are mapped to Spanish messages in the frontend (for example `campaign_name_conflict` → "Ya existe una campaña con ese nombre.").
- This decision does not set the language of imported source content or of future AI answers.

## Consequences

### Positive

- The UI matches its only operator, and new screens are written once.
- Tests assert the real visible copy.

### Negative / accepted trade-offs

- A second language would require extracting strings first.

## Impact on previous decisions

- Supersedes the English visible-copy decision in the Block 01 design record and its roadmap readiness entry.

## Success criterion

- No English visible or accessible copy remains in the shipped screens; unit and E2E tests assert Spanish copy.

## Future review

Review if Lorekeeper gains a second operator or a deployment for a non-Spanish-speaking audience.
