# Campaign MVC Controllers

## Objective

Refactor the Campaign HTTP Presentation layer from Minimal API endpoint adapters to thin, HTTP-operation-oriented MVC controllers while preserving the existing public Campaign API.

## Problem and rationale

Issue #15 identifies unnecessary navigation and registration ceremony in the current use-case-per-endpoint Minimal API structure. CQRS remains inside Application; Presentation should be organized around cohesive HTTP operations and centralize HTTP Problem Details translation.

## Authorized scope

- GitHub Issue #15: https://github.com/danilopgon/lorekeeper/issues/15
- Campaign Presentation, API composition, focused tests, backend architecture documentation, and ADRs needed to record the durable convention.
- Preserve routes, payloads, stable Problem Details codes, MediatR handlers, and transport/Application DTO separation.

## Constraints

- Strict TDD is enabled. Test runner: `dotnet test` (xUnit).
- Do not change Campaign product behavior.
- Prefer ASP.NET Core built-in primitives for Problem Details and exception handling; do not add a generic controller framework or base controllers.
- Do not start Docker without explicit authorization. Testcontainers failures caused by unavailable Docker must be recorded honestly.

## Delivery

- Strategy: `exception-ok` (maintainer-approved `size:exception`).
- Forecast: approximately 500–700 authored changed lines across implementation, regression coverage, and architecture documentation.
- Current slice boundary: one intentionally oversized, coherent PR; avoid artificial splits that would separate tests, migration, and durable architecture documentation.

## Tasks

- [x] MVC-01 — Define MVC routing, metadata, error-contract, and unexpected-failure regression coverage (RED), replacing Minimal API composition-only unit coverage where needed.
  - Route: delegated direct.
  - Trigger: implementation spans tests, API composition, and Presentation files.
  - Acceptance: focused tests demonstrate the intended controller boundary and retain public HTTP contract expectations.
  - Checks: focused unit and integration `dotnet test` commands; observed RED before implementation.

- [x] MVC-02 — Replace Campaign Minimal API adapters/composition with thin operation-oriented MVC controllers; centralize expected and unexpected Problem Details handling through ASP.NET Core primitives while retaining MediatR dispatch and API behavior.
  - Route: delegated direct.
  - Trigger: multi-file non-trivial backend migration requiring write preparation.
  - Acceptance: all three Campaign routes, response contracts, route names, location, and stable error codes remain compatible.
  - Checks: focused GREEN test commands, formatting verification, build, and applicable integration coverage.

- [x] MVC-03 — Record the MVC Presentation convention in the architecture documentation and ADR trail.
  - Route: delegated direct with the implementation work unit.
  - Trigger: coordinated code, documentation, and ADR updates.
  - Acceptance: documentation no longer states MVC is excluded and accurately separates HTTP-oriented Presentation from CQRS-oriented Application.
  - Checks: documentation readback and relevant architecture checks.

## Progress and evidence

- 2026-09-29: Issue #15 mapped. Current branch: `feat/campaign-mvc-controllers`.
- 2026-09-29: Maintainer approved a single PR with `size:exception`.
- Existing implementation baseline: commit `d04b917` (`refactor(api): adopt campaign MediatR presentation adapters (#14)`).
- MVC-01 RED: `dotnet test services/api/tests/Unit/Unit.csproj --filter "FullyQualifiedName~CampaignEndpointCompositionTests" --no-restore` failed with three assertions before implementation: no MVC route endpoints, no controller action metadata, and no `UnexpectedExceptionHandler` type.
- MVC-01/MVC-02 GREEN: `dotnet test services/api/tests/Unit/Unit.csproj --filter "FullyQualifiedName~CampaignsControllerTests" --no-restore` passed 2/2 tests after implementation. It verifies the three named controller actions and the unexpected-error handler's `500`/`unexpected_error` Problem Details contract.
- MVC-02 focused integration: `dotnet test services/api/tests/Integration/Integration.csproj --filter "FullyQualifiedName~CampaignEndpointsTests" --no-restore` was blocked: all 22 tests failed before execution because Testcontainers could not connect to `npipe://./pipe/docker_engine`. Docker was not started.
- Final checks: `dotnet restore services/api/Lorekeeper.slnx` succeeded with two existing SSH.NET `NU1903` warnings; `dotnet format services/api/Lorekeeper.slnx --verify-no-changes` succeeded with workspace-load warnings; `dotnet build services/api/Lorekeeper.slnx --no-restore --configuration Release` succeeded with the same two `NU1903` warnings; `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` was partial because 27 of 28 integration tests were blocked by the unavailable Docker endpoint. No product assertion failure was observed.
- Decision: use one `CampaignsController` for list/create/get, retain private-to-Presentation transport mapping through its nested `CreateCampaignRequest`, centralize expected results in `CampaignProblemDetails`, and use ASP.NET Core `IExceptionHandler`/`IProblemDetailsService` for unexpected failures. No generic controller abstraction was added.
- Rollback boundary: revert the MVC controller, focused Problem Details helper, exception handler, API composition registrations, controller regression test, and ADR/documentation updates together; MediatR messages, handlers, persistence, and public Campaign contracts remain unchanged.
- Work-unit commit: pending final commit.

## Next step

Record the final work-unit commit identity; Docker-backed integration verification remains blocked until Docker is available.
