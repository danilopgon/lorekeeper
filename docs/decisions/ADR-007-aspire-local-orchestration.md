# ADR-007 — Aspire AppHost for local development orchestration

**Status:** Accepted
**Date:** 2026-11-03
**Area:** operations

## Context and problem

Running Lorekeeper locally takes four manual steps (`db:start`, `api:migrate`, `api:start`, `web:start`) across several terminals, and there is no shared way to see logs or restart one process. 04A adds ingestion jobs, which makes this worse. The operator wants one command and per-process control.

The #20 spike tested Aspire 13.6.4 against the real stack on 2026-11-03.

| Measure | Aspire | Current flow |
| --- | --- | --- |
| Time until the web proxy serves `/api/campaigns` | 25 s (35 s on first build) | 26 s |
| Commands | 1 | 4 |
| Restart one process | Yes (dashboard or `aspire resource`) | By hand |
| Extra memory | About 400 MB | None |

Playwright passed 2/2 against the Aspire stack with one worker.

## Alternatives considered

### Option A — `concurrently` behind `pnpm dev`

- Benefits: small; prefixed logs; stops all processes when one exits.
- Costs/risks: no restart of a single process; no dashboard; does not grow into telemetry.

### Option B — `mprocs`

- Benefits: terminal UI with per-process restart.
- Costs/risks: another tool that Aspire would replace if adopted later. Not tested.

### Option C — `vp run` (Vite+)

- Benefits: task dependencies and failure handling worked in the #19 spike.
- Costs/risks: 1,120 lockfile lines and a `vite.config.ts` in an application that does not build with Vite. #19 was rejected.

### Option D — Aspire AppHost, for local development only

- Benefits: one command, startup ordering, per-resource logs and restart, and a path to telemetry.
- Costs/risks: an extra project, a global CLI, and about 400 MB of memory.

## Decision

Use **Option D**, for local development only. It covers process control without adding a tool that would later be discarded.

## Proposed design

- `services/api/src/AppHost` orchestrates PostgreSQL 17, an EF Core migration step, the API and the Angular dev server. `aspire.config.json` points the CLI at it from the repository root.
- `pnpm run dev` stops the Compose database and runs `aspire run`.
- The AppHost reuses the Compose volume `lorekeeper_postgres-data`, so both flows see the same data. **Never run both at once on that volume**: stop one before starting the other.
- The Angular dev server keeps port 4200 and its proxy; the API keeps port 5044 through its `http` launch profile.
- The existing pnpm scripts and Docker Compose remain as the supported fallback. Playwright and CI do not depend on Aspire.
- Compose publishes PostgreSQL on `127.0.0.1` only.

### Development credentials

The AppHost uses the same known development user and password as `docker-compose.yml` and `appsettings.Development.json`. The repository is public, so this value is not a secret and protects nothing by itself: the database must stay bound to loopback, and no deployed environment may use it. Parameters are pinned because a generated password stopped matching an already initialized volume during the spike, which blocked the second start. `secret: true` only masks the value in the dashboard.

## Consequences

### Positive

- One command starts the system, and one process can be restarted without touching the others.
- Logs per resource are available from the dashboard and `aspire logs`.

### Negative / accepted trade-offs

- Developers need the Aspire CLI and Docker for `pnpm run dev`.
- Memory use rises by about 400 MB.
- A crashed resource is not restarted automatically, and the web resource waits for the API, so after both fail the API restarts first.
- Traces need `ServiceDefaults` in the API and are not enabled by this decision.

## Impact on previous decisions

- ADR-002 is unchanged: this adds no Internet exposure and is not a deployment architecture.
- Refines the local database choice in `docs/04-architecture.md` and `docs/09-delivery.md`; Compose remains available.

## Success criterion

- `pnpm run dev` from the repository root serves the app with existing data, stopping it leaves no processes, containers or bound ports, and the Compose fallback still reads the same data.

## Future review

Review when 04A adds background work or AI providers that need telemetry, or if the memory overhead becomes a measured problem. The optional YARP and single-origin comparison and the architecture review in #20 were not part of this spike; open them only if a concrete need appears.
