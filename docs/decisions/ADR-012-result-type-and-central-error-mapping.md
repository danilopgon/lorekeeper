# ADR-012 — Result type with typed errors, mapped to Problem Details in one place

**Status:** Accepted
**Date:** 2026-11-18
**Area:** backend

## Context and problem

Each use case returned its own status enum (`CreateCampaignStatus`, `GetCampaignStatus`, and in #43 `PasteSourceStatus` and `ListSourcesStatus`), and each controller translated it with a `switch` into a module-specific Problem Details factory. Value objects returned a separate `DomainResult<T>`. Block 04A adds about six more Sources endpoints, each with several expected failures, so the enums, switches and factories would multiply. The maintainer raised it in the review of #43 and decided on 2026-11-18.

## Alternatives considered

### Option A — Keep status enums and controller switches

- Benefits: explicit per endpoint; no shared abstraction.
- Costs/risks: one enum, one switch and one factory per use case; a new failure needs edits in three places.

### Option B — A library such as ErrorOr or FluentResults

- Benefits: mature types and helpers.
- Costs/risks: a new dependency and its vocabulary for a type of about fifty lines; error kinds would still be modelled by us.

### Option C — Project-owned `Result<T>` with typed errors and one mapping

- Benefits: one return shape for every handler; one place decides the HTTP status; adding a failure means adding an `Error`.
- Costs/risks: the mapping becomes shared HTTP infrastructure, which `AGENTS.md` asks to justify with an ADR.

## Decision

Use **Option C** in Campaign and Sources.

## Proposed design

- `Api.SharedKernel.Result<T>` holds a value or an `Error`. `Error` carries a stable public `Code` (the `code` in `06`), a `Kind` (`Validation`, `NotFound`, `Conflict`), a `Message` used as the Problem Details `detail`, and optional `Fields` that become the `errors` map. `Error.InvalidFields` reports several invalid fields at once; its `Code` is the first field's.
- Value objects return `Result<T>` too; `DomainResult<T>` is removed.
- Handlers return `Task<Result<T>>`, including queries that cannot fail today, so every controller action has the same shape.
- Controllers call `result.Match(value => …, error => error.ToProblem())`. `Api.Common.Presentation.ErrorProblemDetails.ToProblem` maps `Validation` to 400, `NotFound` to 404 and `Conflict` to 409. Success status codes (`200`, `201` with `Location`, `202`) stay in each controller.
- Unexpected failures are not `Error`s: they keep propagating to the shared `unexpected_error` handler (ADR-008).
- The HTTP contract does not change: status codes, `code` values, `errors` maps, `detail` texts and payloads stay identical.
- Architecture tests fail when an Application namespace declares a `*Status` enum or a `Handle` method does not return `Task<Result<T>>`.

## Consequences

### Positive

- No per-endpoint status enums, switches or Problem Details factories.
- Field errors from several value objects combine into one response without bespoke code.

### Negative / accepted trade-offs

- Queries that cannot fail are wrapped in a `Result`.
- `Error` messages live next to their codes in Domain and Application, not in Presentation.

## Impact on previous decisions

- **Refines ADR-003 and ADR-004.** MediatR dispatch and MVC controllers stay; the handler response type becomes `Result<T>` and the Campaign Problem Details mapper is replaced by the shared mapping.
- **Refines ADR-008.** Repositories still translate expected PostgreSQL errors into outcomes that handlers turn into `Error`s.
- Related layout changes in the same refactor (#44), recorded in `04`: persistence code in `Infrastructure/Persistence/`, and the API's `migrate` command (ADR-007).

## Success criterion

- Campaign and Sources controllers contain no `switch` over results, and the committed OpenAPI document is unchanged by the migration.

## Future review

Review if an error needs an HTTP status outside the three kinds, or if a non-HTTP consumer needs a different mapping.
