# ADR-013 — MediatR dispatch for every module

**Status:** Accepted
**Date:** 2026-11-18
**Area:** backend

## Context and problem

ADR-003 adopted MediatR for Campaign only, and `04` required an ADR before another module used it. The first Sources slice (#43) therefore injected handler classes straight into its controllers. That left two dispatch styles side by side. On 2026-11-18, while reviewing #43, the maintainer chose one dispatch style for all modules.

## Alternatives considered

### Option A — MediatR only in Campaign; plain handlers elsewhere

- Benefits: no dependency on MediatR outside Campaign.
- Costs/risks: two ways to call a use case; controllers depend on concrete handler types.

### Option B — MediatR in every module

- Benefits: one shape for every use case (`IRequest<Result<T>>` and `IRequestHandler`), and controllers depend only on `ISender`. Registration happens once.
- Costs/risks: MediatR becomes a dependency of every module's Application layer.

## Decision

Use **Option B**.

## Proposed design

- Every HTTP use case is a request implementing `IRequest<Result<T>>`, handled by an `IRequestHandler` (ADR-012).
- Controllers inject `ISender`; they never inject handler classes.
- The API composition root registers MediatR once, scanning the API assembly. Modules no longer call `AddMediatR`.
- Background processing that is not an HTTP use case, such as the Sources ingestion worker's `ISourceVersionProcessor`, stays a port that the worker calls directly.
- Pipeline behaviors, generic base handlers and notifications remain excluded, as in ADR-003.
- An architecture test fails when an Application type has a public `Handle` without implementing `IRequestHandler<,>`, or when a Presentation type injects a handler.

## Consequences

### Positive

- One dispatch style across modules.
- Handlers can be unit tested without controllers, and controllers stay thin.

### Negative / accepted trade-offs

- Navigating from a controller to its handler goes through the request type.

## Impact on previous decisions

- **Refines ADR-003.** Its MediatR adoption now applies to every module. Its exclusions stand.
- Replaces the rule in `04` and `AGENTS.md` that required an ADR before another module adopted MediatR.

## Success criterion

- Sources controllers dispatch through `ISender`, and the architecture test passes with no exceptions.

## Future review

Review if a module needs in-process events or cross-cutting behaviors that MediatR pipelines would hide.
