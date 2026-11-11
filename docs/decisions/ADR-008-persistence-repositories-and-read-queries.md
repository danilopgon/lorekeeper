# ADR-008 — Repositories for commands and dedicated read interfaces for queries

**Status:** Accepted
**Date:** 2026-11-11
**Area:** backend

## Context and problem

Campaign handlers depend directly on `CampaignsDbContext` and EF Core (#18). That couples Application to persistence, although `04-architecture.md` says Domain and Application do not depend on EF Core. Block 04A adds Sources, whose commands need a real transactional boundary: a new source version, its chunks, its ingestion job and the published-version pointer must commit together or not at all (ADR-009, `03-domain-model.md`).

ADR-003 and ADR-004 excluded repositories because Campaign had no demonstrated need. The maintainer decided on 2026-11-11 that Sources and Campaign share one persistence pattern, and that Campaign adopts it first.

## Alternatives considered

### Option A — DbContext in handlers, as Campaign does today

- Benefits: no new interfaces; one `SaveChanges` is already atomic.
- Costs/risks: Application keeps depending on EF Core; database error translation stays mixed with use-case logic.

### Option B — Repository for commands and reads

- Benefits: Application never sees EF Core.
- Costs/risks: the repository grows one method per screen and invites loading whole aggregates (including 1 MiB source texts) to read three columns.

### Option C — Repository for commands, reads projected from the DbContext

- Benefits: cheapest CQRS split; efficient projections.
- Costs/risks: Application still references EF Core on the read side.

### Option D — Repository for commands, dedicated read interface for queries

- Benefits: Application is free of EF Core; reads stay purpose-built projections; read and write paths are explicit.
- Costs/risks: two interfaces and two implementations per module; read methods grow with screens.

## Decision

Use **Option D** in both Campaign and Sources.

## Proposed design

- **Commands** depend on a module-specific repository owned by Application, for example `ICampaignRepository` and `ISourceRepository`. Each exposes only the operations its aggregate needs (load by id within a campaign, add, save). There is no generic `IRepository<T>`.
- **Queries** depend on a module-specific read interface, for example `ICampaignQueries` and `ISourceQueries`, which return DTOs or read models directly. They never hydrate aggregates.
- **Infrastructure** implements both with the module's EF Core `DbContext`. The repository's save method owns `SaveChangesAsync`, the transaction for that unit of work and the translation of PostgreSQL errors (such as unique violations) into application results. Npgsql and EF exceptions do not reach Application.
- **Campaign migrates first**, in its own PR before block 04A, with unchanged routes, payloads, `201 Location` and Problem Details codes.
- Registration happens in each module (`CampaignsModule`, and the future Sources module).

## Consequences

### Positive

- Application layers no longer reference EF Core, as `04` already requires.
- Sources gets one explicit place for its multi-entity transaction.
- Query handlers can be unit tested against a substitute read interface; persistence stays covered by Testcontainers integration tests.

### Negative / accepted trade-offs

- More files per module than Option A.
- Read interfaces gain methods as screens are added; keep them per module and per use case, not shared.

## Impact on previous decisions

- **Refines ADR-003 and ADR-004.** Their "no repositories" exclusion is replaced for Campaign and Sources by this design. Their MediatR dispatch and MVC presentation decisions are unchanged, and pipeline behaviors, generic base handlers and generic repositories remain excluded.
- Updates the pragmatic CQRS rules and non-goals in `docs/04-architecture.md` and the CQRS rule in `AGENTS.md`.

## Success criterion

- Campaign and Sources command handlers reference neither `Microsoft.EntityFrameworkCore` nor `Npgsql`; query handlers depend only on read interfaces; existing Campaign integration tests pass unchanged.

## Future review

Review if a module needs a transaction spanning two repositories, or if read interfaces start duplicating the same projection across use cases.
