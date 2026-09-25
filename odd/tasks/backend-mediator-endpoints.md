# Backend Mediator Endpoints

## Objective

Migrate the existing Campaigns HTTP-to-application dispatch boundary to MediatR and correct its HTTP adapter organization to use-case-oriented presentation files, while preserving Campaign API behavior.

## Problem and rationale

Campaign endpoint delegates inject concrete application handlers directly. As the module grows, this couples HTTP adapters to use-case implementation types and leaves one endpoint mapper responsible for unrelated route logic. Issue #13 requests an explicit command/query dispatch boundary and a documented decision.

## Authorized scope

- Issue #13 only: Campaigns endpoint architecture, MediatR command/query dispatch, use-case-oriented presentation adapters, tests, and supporting architecture documentation.
- Preserve existing Campaign HTTP routes, response bodies, OpenAPI contracts, and Problem Details codes.
- Do not add repositories, generic base handlers, MVC controllers, unrelated frontend work, or future capability scaffolding.

## Constraints

- Strict TDD mode: `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release`.
- Route: delegated direct. Trigger: implementation spans multiple non-trivial backend, test, and documentation files.
- Delivery strategy: ask-on-risk; forecast: one correction work unit, likely over the advisory 400 authored-line heuristic because it moves existing adapters and aligns their focused tests and documentation without changing behavior.

## Tasks

- [ ] BE-01 — Correct ADR-003 and aligned architecture guidance to accept use-case-oriented Campaign presentation adapters. Reopened reason: the earlier single `CampaignEndpoints.cs` rationale contradicted Issue #13's preferred HTTP adapter structure and is invalid. Route: delegated. Acceptance: documentation records the accepted physical presentation split, bounded composition responsibility, and rejected generic framework. Checks: documentation readback. Prior evidence: `455a30e` (`feat(api): dispatch campaign endpoints with MediatR`) is superseded only for endpoint organization.
- [ ] BE-02 — Correct Campaign create/list/get HTTP adapter organization without changing MediatR dispatch or public behavior. Reopened reason: the completed implementation retained the large single `CampaignEndpoints.cs`, so it did not meet Issue #13's preferred use-case-oriented HTTP adapter structure. Route: delegated. Acceptance: endpoints use `ISender` from physical presentation adapters; composition owns only `/api/campaigns` group setup; no generic abstractions are introduced. Checks: strict TDD RED/GREEN evidence; backend build and focused tests. Prior evidence: `455a30e` remains valid for MediatR requests/handlers, not presentation organization.
- [x] BE-03 — Extend HTTP/application boundary coverage, run applicable backend checks, and reconcile task documentation. Route: delegated. Acceptance: Campaign behavior and Problem Details remain covered; all recorded checks have observed outcomes. Checks: formatter, Release build, focused Campaign tests, full backend suite. Evidence: `dotnet format services/api/Lorekeeper.slnx --verify-no-changes` passed; Campaign integration coverage is retained in `CampaignEndpointsTests`, but `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release --filter FullyQualifiedName~Campaign` was blocked because Docker was unavailable at `npipe://./pipe/docker_engine` (19 Unit tests passed; 27 Testcontainers integration tests could not start). `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` was likewise blocked (20 Unit tests passed; 27 integration tests could not start). Commit: `455a30e` (`feat(api): dispatch campaign endpoints with MediatR`). Rollback: remove `CampaignRequestHandlerTests.cs`; no public contract test changes were made.
- [ ] BE-04 — Split Campaign HTTP adapters into a physical `Presentation/` layer and prove composition maps them without changing HTTP behavior. Reason: corrective implementation for Issue #13 after ADR-003's invalid single-file rationale. Route: delegated. Acceptance: create/list/get endpoint definitions own metadata, binding, `ISender` dispatch, and result mapping; composition owns group setup only; focused tests preserve routes, names, OpenAPI metadata, DTOs, status/body, and Problem Details codes. Checks: observed RED before implementation; formatter, Release build, focused Campaign tests, and full backend suite. Rollback: revert only the presentation file moves, composition mapper, focused split tests, and architecture correction; keep the prior MediatR requests/handlers.

## Progress and evidence

- Current branch: `feat/backend-mediator-endpoints`.
- Source baseline: `ef731f3`.
- Correction in progress: BE-01, BE-02, and BE-04 are reopened before source edits. The prior completed behavior evidence remains historical; new evidence and one coherent correction commit will be recorded after observed RED/GREEN and verification.
