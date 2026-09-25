# ADR-003 — MediatR dispatch for Campaign use cases

**Status:** Accepted

**Date:** 2026-09-25

**Area:** Backend

## Context and problem

The Campaign Minimal API delegates injected concrete create, list and get handler types directly. This couples each HTTP adapter to an application implementation and makes the endpoint mapper own both transport mapping and use-case selection. The three existing use cases form a demonstrated, coherent dispatch boundary without changing their public HTTP contracts.

## Alternatives considered

### Keep direct handler injection

- Benefits: no new dependency.
- Costs/risks: HTTP adapters remain coupled to concrete application handlers as the module gains use cases.

### Introduce MediatR for Campaign use cases

- Benefits: endpoints depend on `ISender` and explicit request/handler contracts while preserving Minimal APIs and current public mappings.
- Costs/risks: one in-process dependency and an indirect handler dispatch path.

### Add repositories, generic handlers, pipeline behaviors, or MVC controllers

- Benefits: could provide abstractions for future needs.
- Costs/risks: no demonstrated Campaign need; adds ceremony, hides EF Core use and expands the transport refactor beyond Issue #13.

## Decision

Use MediatR for the Campaign create command and list/get queries. Register handlers by scanning the API assembly. Endpoint delegates inject `ISender`, construct application messages and retain all HTTP status, response and Problem Details mapping at the transport boundary.

## Proposed design

- Each Campaign feature folder owns an explicit `IRequest<TResponse>` message and its `IRequestHandler<TRequest, TResponse>` implementation.
- `CampaignEndpoints` remains the single Campaign route mapper because the three routes share the same group and Problem Details helpers; its delegates remain use-case-specific adapters.
- The HTTP body DTO stays private to the endpoint adapter. Application messages are not bound directly from HTTP.
- No pipeline behaviors, repositories, generic base handlers, custom endpoint framework or MVC migration are introduced.

## Consequences

### Positive

- New Campaign HTTP adapters depend on the stable `ISender` boundary rather than concrete handlers.
- Handler tests can assert request/handler contracts independently of HTTP behavior.
- Existing OpenAPI metadata and public API contracts remain unchanged.

### Negative / accepted trade-offs

- MediatR adds a small indirection and package dependency.
- Cross-cutting behaviors remain absent until a concrete policy needs one and an ADR justifies it.

## Impact on previous decisions

- Refines the pragmatic CQRS guidance in `docs/04-architecture.md`; it does not change public API contracts or module ownership.

## Success criterion

- Campaign endpoints dispatch all three use cases through `ISender`, handlers implement their matching MediatR contracts, and Campaign API integration tests retain their established responses and Problem Details codes.

## Future review

Review when a cross-cutting application policy has demonstrated requirements that cannot be met at the current endpoint or handler boundary.
