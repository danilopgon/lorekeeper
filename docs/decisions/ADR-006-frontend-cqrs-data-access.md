# ADR-006 — Native Angular CQRS data access in the frontend

**Status:** Accepted
**Date:** 2026-10-31
**Area:** frontend

## Context and problem

Block 02 replaces the handwritten Block 01 campaign adapter with code driven by the generated OpenAPI contract. Before it does, the frontend needs a durable shape for remote data: reads and writes must stay separate (the same CQRS split the backend uses), responses must be validated at the boundary (AGENTS.md), and the shape must survive 04A, which adds ingestion progress and source lists that change after commands.

The current adapter exposes `Promise<T>` methods, reads go through `resource` and commands are wrapped with RxJS `from(...)` in components. Validation is hand-written type guards that can drift from the backend contract.

TanStack Query is the main alternative. Its Angular adapter is still published as `@tanstack/angular-query-experimental` (5.105.0 on 2026-10-26); the stable `@tanstack/angular-query` package is a `0.0.0` placeholder, and minor or patch releases may break the API.

## Alternatives considered

### Option A — Native: `httpResource` queries and `HttpClient` commands

- Benefits: no extra runtime dependency; signals-first and zoneless; `httpResource` has a `parse` option designed for schema validation; keeps Angular interceptors and `HttpTestingController`.
- Costs/risks: no shared query cache, deduplication, retries or polling; invalidation after a command is written by hand.

### Option B — TanStack Query with the generated `fetch` client

- Benefits: shared cache with query keys, invalidation, `refetchInterval` polling and mutation state out of the box; hey-api can generate the fetch client, Zod-validated SDK and `queryOptions`/`mutationOptions`.
- Costs/risks: experimental Angular adapter and a 0.x generator, both pinned to exact versions; drops `HttpClient` and its testing tools; little benefit for the current three campaign endpoints.

### Option C — TanStack Query wrapping `HttpClient`

- Benefits: keeps Angular HTTP tooling under TanStack's cache.
- Costs/risks: mixes Observables and promises (`firstValueFrom` in every query) and carries the experimental dependency without the generated query layer.

## Decision

Use **Option A**. It covers the current endpoints with no new runtime dependency besides Zod, and keeps the data layer shaped so that moving to Option B later only touches each feature's `api/` folder.

## Proposed design

- The generated contract (`06`) supplies TypeScript types and Zod schemas only: no generated SDK, services or resources.
- **Queries** are `inject<Noun>Query(...)` functions in `<feature>.queries.ts`. Each one wraps one GET in `httpResource` with `parse: z<Dto>.parse`, takes its parameters as signals (an `undefined` value means no request) and never changes server state. They are called from component field initializers so the resource is owned by the component's injector.
- **Commands** are methods named `<verb><Noun>` on an `@Injectable` `<Feature>Commands` class in `<feature>-commands.service.ts`. Each method calls `HttpClient`, validates the response with the generated schema and returns `Promise<Model>` or `Promise<void>`. Command services hold no state.
- Transport errors (`HttpErrorResponse`, `ZodError`, Problem Details) are mapped to the feature's API error type in `api/<feature>-api-error.ts`. Commands throw the mapped error; pages expose query errors mapped through a `computed`.
- Invalidation is explicit: the code that runs a command reloads the queries it affects (`resource.reload()`).
- Components consume only `value()`, `isLoading()`, `error()` and `reload()` and command promises. They never use `HttpClient`, URLs or generated schemas directly.

```text
src/app/
├── shared/api/generated/            # hey-api output; never edited by hand
└── features/<feature>/api/
    ├── <feature>.queries.ts
    ├── <feature>-commands.service.ts
    └── <feature>-api-error.ts
```

## Consequences

### Positive

- Reads and writes are separated by file, name and type, matching backend CQRS.
- Validation comes from the backend contract instead of hand-written guards.
- Queries have the same call shape as TanStack's `injectQuery`, so a later migration stays mechanical and local to `api/`.

### Negative / accepted trade-offs

- No shared cache: two components reading the same data issue two requests unless the query is lifted to a common parent.
- Polling and cross-view invalidation are hand-written when a feature needs them.
- Command orchestration in components uses `async`/`await` instead of RxJS operators.

## Impact on previous decisions

- Replaces the Block 01 "Angular async boundaries" convention in `docs/conventions/frontend.md` (single `Promise` adapter read through `resource`, commands wrapped with `from(...)`).

## Success criterion

- The campaign feature has no handwritten DTO guards; its queries use `httpResource` with generated schemas, its commands live in `CampaignCommands`, and no page or component imports `HttpClient`.

## Future review

Review when TanStack publishes a stable Angular adapter, or when a block (04A ingestion progress, source lists) needs polling or cross-view invalidation that becomes costly to hand-write. The expected migration target is Option B.
