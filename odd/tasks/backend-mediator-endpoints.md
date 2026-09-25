# Backend Mediator Endpoints

## Objective

Migrate the existing Campaigns HTTP-to-application dispatch boundary to MediatR while preserving Campaign API behavior and keeping Minimal APIs as thin transport adapters.

## Problem and rationale

Campaign endpoint delegates inject concrete application handlers directly. As the module grows, this couples HTTP adapters to use-case implementation types and leaves one endpoint mapper responsible for unrelated route logic. Issue #13 requests an explicit command/query dispatch boundary and a documented decision.

## Authorized scope

- Issue #13 only: Campaigns endpoint architecture, MediatR command/query dispatch, tests, and supporting architecture documentation.
- Preserve existing Campaign HTTP routes, response bodies, OpenAPI contracts, and Problem Details codes.
- Do not add repositories, generic base handlers, MVC controllers, unrelated frontend work, or future capability scaffolding.

## Constraints

- Strict TDD mode: `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release`.
- Route: delegated direct. Trigger: implementation spans multiple non-trivial backend, test, and documentation files.
- Delivery strategy: ask-on-risk; forecast: three work units, likely under the advisory 400 authored-line heuristic per unit.

## Tasks

- [x] BE-01 — Record the MediatR boundary decision in an ADR and align backend architecture/roadmap guidance. Route: delegated. Acceptance: documented demonstrated benefit, scope, endpoint organization, and rejected alternatives. Checks: documentation readback passed. Evidence: ADR-003 records the decision, endpoint organization, alternatives and non-goals; `AGENTS.md`, `docs/04-architecture.md`, `docs/10-roadmap.md` and the ADR index align with it. Commit: `455a30e` (`feat(api): dispatch campaign endpoints with MediatR`).
- [x] BE-02 — Add MediatR composition and migrate Campaign create/list/get use cases to explicit commands/queries and handlers, with use-case-oriented Minimal API adapters. Route: delegated. Acceptance: endpoints use `ISender`, public behavior remains stable, and no generic abstractions are introduced. Checks: strict TDD RED/GREEN evidence; backend build and focused tests. Evidence: RED: `dotnet test services/api/Lorekeeper.slnx --configuration Release --filter FullyQualifiedName~CampaignRequestHandlerTests` failed with CS0246 for the three absent request types after restore. GREEN: the same command passed 1 Unit test after implementation. `dotnet build services/api/Lorekeeper.slnx --no-restore --configuration Release` passed with 0 errors (2 pre-existing SSH.NET NU1903 warnings). Commit: `455a30e` (`feat(api): dispatch campaign endpoints with MediatR`). Rollback: remove the MediatR package, request/handler interfaces and endpoint `ISender` dispatch changes as one bounded Campaign adapter change.
- [x] BE-03 — Extend HTTP/application boundary coverage, run applicable backend checks, and reconcile task documentation. Route: delegated. Acceptance: Campaign behavior and Problem Details remain covered; all recorded checks have observed outcomes. Checks: formatter, Release build, focused Campaign tests, full backend suite. Evidence: `dotnet format services/api/Lorekeeper.slnx --verify-no-changes` passed; Campaign integration coverage is retained in `CampaignEndpointsTests`, but `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release --filter FullyQualifiedName~Campaign` was blocked because Docker was unavailable at `npipe://./pipe/docker_engine` (19 Unit tests passed; 27 Testcontainers integration tests could not start). `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` was likewise blocked (1 Unit test passed; 27 integration tests could not start). Commit: `455a30e` (`feat(api): dispatch campaign endpoints with MediatR`). Rollback: remove `CampaignRequestHandlerTests.cs`; no public contract test changes were made.

## Progress and evidence

- Current branch: `feat/backend-mediator-endpoints`.
- Source baseline: `ef731f3`.
- Completed locally with integration verification blocked by unavailable Docker. Next step: record the work-unit commit hash, mirror this document to Engram, and rerun Campaign and full backend integration tests when Docker is available.
