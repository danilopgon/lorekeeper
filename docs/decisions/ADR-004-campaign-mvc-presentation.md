# ADR-004 — MVC controllers for Campaign HTTP presentation

**Status:** Accepted

**Date:** 2026-09-29

**Area:** Backend

## Context and problem

ADR-003 introduced MediatR for the Campaign create command and list/get queries while retaining Minimal API adapters. As the three routes are a cohesive HTTP resource, separate route-mapping files and a module composition mapper now add navigation and registration ceremony without adding a domain or application boundary. Expected application results and unexpected exceptions also need one consistent RFC 7807 translation path.

## Alternatives considered

### Keep Minimal API adapters and the composition mapper

- Benefits: preserves the current physical layout.
- Costs/risks: one HTTP resource is split across route registration and three small files, and the custom exception middleware duplicates framework facilities.

### Use one resource-oriented MVC controller

- Benefits: keeps related HTTP operations, route metadata, transport binding and `ISender` dispatch together while retaining separate HTTP and Application DTOs.
- Costs/risks: adds MVC service and endpoint composition to the API root.

### Add a generic base controller or endpoint framework

- Benefits: could standardize future controller code.
- Costs/risks: no demonstrated cross-module need; hides transport decisions and increases framework ceremony.

## Decision

Use `CampaignsController` as the Campaign HTTP adapter. It owns the existing list, create and get operations, their route names and OpenAPI response metadata. Each action converts its HTTP request DTO into an explicit MediatR request and retains no application orchestration logic.

Use a Campaign-specific Problem Details mapper for expected application results. Register ASP.NET Core `AddProblemDetails`, `AddExceptionHandler<UnexpectedExceptionHandler>` and `AddControllers` in the API composition root; execute `UseExceptionHandler` before mapped endpoints and use `MapControllers` to expose the controller. The exception handler writes the established `unexpected_error` RFC 7807 contract through `IProblemDetailsService`.

Do not introduce a generic controller, base controller, custom endpoint framework, MediatR pipeline behavior, repository, or wrapper interface.

## Consequences

### Positive

- Campaign HTTP operations are discoverable in one controller without changing their public routes, names, payloads, `201 Location`, or stable Problem Details codes.
- Expected and unexpected failures use ASP.NET Core Problem Details primitives with explicit, testable contract mapping.
- MediatR remains the narrow Application dispatch boundary selected by ADR-003.

### Negative / accepted trade-offs

- The controller remains responsible for explicit transport-to-message translation.
- MVC action metadata must be kept aligned with the documented OpenAPI contract.

## Impact on previous decisions

- Refines ADR-003's presentation-only constraint: Campaign use cases continue to dispatch through `ISender`, but their HTTP adapters are MVC controller actions rather than Minimal API delegates.
- Does not change Campaign domain rules, application messages, persistence, public API contracts, or roadmap readiness.

## Success criterion

- `AddControllers` and `MapControllers` compose the API; `CampaignsController` maps exactly the established Campaign list/create/get route names; expected RFC 7807 codes and the unexpected `unexpected_error` contract remain stable; and focused unit plus Campaign API integration coverage verifies the boundary.
