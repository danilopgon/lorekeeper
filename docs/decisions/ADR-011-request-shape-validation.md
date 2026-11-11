# ADR-011 — FluentValidation for request shape only

**Status:** Accepted
**Date:** 2026-11-11
**Area:** backend

## Context and problem

Issue #22 asked whether FluentValidation improves command validation. Campaign validates through the `CampaignName` value object and typed results. Block 04A adds import, paste, update, metadata edit, retry and removal commands. Their rules fall into three groups:

| Group | Examples | Needs the database |
| --- | --- | --- |
| Domain invariants | Title 1–120 characters, size up to 1 MiB, valid UTF-8, knowledge status value | No |
| Request shape | Multipart batch not empty, at most 50 files, `metadata` part present and parseable, one metadata entry per file | No |
| State and existence | Campaign exists, source belongs to the campaign, no update while a job is running, source not removed | Yes |

## Alternatives considered

### Option A — Value objects and typed results only

- Benefits: no dependency; one place per rule.
- Costs/risks: request-shape checks become ad-hoc code in controllers or handlers, repeated per endpoint.

### Option B — Explicit FluentValidation validators for request shape

- Benefits: declarative, testable request-shape rules; domain stays free of the library.
- Costs/risks: one dependency; risk of duplicating domain rules if the boundary is not kept.

### Option C — FluentValidation in a MediatR pipeline behavior

- Benefits: automatic for every command.
- Costs/risks: `04` requires demonstrated need for pipeline behaviors; hides validation order; tempts moving domain rules into validators.

## Decision

Use **Option B**, limited to request shape. Close #22 with this decision.

## Proposed design

- Domain invariants stay in value objects and return typed domain results, as `CampaignName` does.
- State and existence rules stay in handlers, which query through ADR-008 interfaces and return typed results.
- Request-shape rules live in `IValidator<T>` classes next to the command, invoked explicitly before dispatch or at the start of the handler. They never repeat a value-object rule.
- Request-shape failures reject the whole request with `400` Problem Details and stable codes (`06`). Per-file checks (extension, size, UTF-8, empty text, title, category, knowledge status) are not request shape: in a batch import they produce per-item outcomes inside the `202` response. Domain and Application results do not reference FluentValidation types.
- `CreateCampaignCommand` keeps its current validation; it has no request-shape rule worth a validator.
- No MediatR pipeline behavior is added.

## Consequences

### Positive

- Each rule has exactly one home, chosen by whether it needs the database or belongs to the domain.

### Negative / accepted trade-offs

- A new package (`FluentValidation`) in the API.
- Reviewers must check that validators do not absorb domain rules.

## Impact on previous decisions

- Refines the validation guidance in `04-architecture.md`. ADR-003's exclusion of pipeline behaviors is unchanged.

## Success criterion

- 04A request-shape rules are covered by validator unit tests, domain rules by value-object tests, and both map to the documented error codes in integration tests.

## Future review

Review if several modules need identical validation ordering that explicit invocation makes repetitive.
