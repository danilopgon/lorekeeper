# ADR-009 — PostgreSQL-backed job queue for source ingestion

**Status:** Accepted
**Date:** 2026-11-11
**Area:** backend

## Context and problem

Block 04A imports Markdown and text into campaign sources. Reading, chunking and storing a document takes milliseconds today, but 05B adds embedding generation through an AI provider, which is slow and can fail. The maintainer decided on 2026-11-11 to process imports as background jobs from 04A onward, so that the source lifecycle, progress and retry behaviour are built once.

The key constraint is atomicity: a job must be created in the same transaction as the `SourceVersion` it processes. If the version commits without its job, it stays "queued" forever; if the job commits without its version, it fails on a missing row.

## Alternatives considered

### Option A — Synchronous processing per document

- Benefits: no queue, no job states.
- Costs/risks: 05B would have to introduce jobs anyway; rejected by the maintainer.

### Option B — In-memory `Channel<T>`

- Benefits: smallest implementation.
- Costs/risks: pending work is lost when the API restarts; recovery would need a database scan anyway.

### Option C — Hangfire

- Benefits: mature, dashboard, automatic retries.
- Costs/risks: stores jobs through its own storage layer and schema; PostgreSQL storage is a community package; enqueueing inside the application's EF Core transaction is not documented in the core docs we checked (atomic batches are a Hangfire Pro feature), so atomicity would rest on unverified `TransactionScope` enlistment. Jobs serialize method calls, which is fragile under refactoring.

### Option D — TickerQ

- Benefits: EF Core persistence, `StageAsync` stages a job in the application's `DbContext` transaction, dashboard and retries.
- Costs/risks: a young dependency oriented to time and cron scheduling; its dashboard would need protection behind Cloudflare Access if deployed.

### Option E — Job table in the application database

- Benefits: no new dependency; trivially atomic with the version through the same `SaveChanges`; durable across restarts.
- Costs/risks: claiming, retries and stuck-job recovery are ours to write and test.

## Decision

Use **Option E**: a job table in PostgreSQL, processed by a hosted `BackgroundService`.

## Proposed design

- A `source_ingestion_jobs` table owned by the Sources module, with `campaignId`, `sourceVersionId`, state (`queued`, `processing`, `succeeded`, `failed`), attempt count, next attempt time, lease expiry and a public error code.
- The command that creates a `SourceVersion` adds its job in the same unit of work (ADR-008).
- Workers claim jobs with `SELECT … FOR UPDATE SKIP LOCKED` and set a lease. A job whose lease expires returns to `queued`, so a crash never leaves it stuck.
- **Retries:** up to 3 attempts with growing delay for transient failures (database or provider). Content errors (invalid UTF-8, empty text) fail immediately without retry. After the final failure the operator can retry manually; a manual retry reprocesses the same version.
- **Idempotency:** processing a version replaces any chunks it already wrote, so a retried job never duplicates chunks.
- **Removal wins:** a job that finds its source removed discards its work without publishing.
- State changes notify the in-process event stream used by SSE (ADR-010).
- Jobs and their spans are traced with OpenTelemetry (`04`).

## Consequences

### Positive

- No new runtime dependency or schema owned by a library.
- One transaction covers version, job and later publication.
- 05B reuses the same queue for embedding work.

### Negative / accepted trade-offs

- No job dashboard; visibility comes from the Sources UI and OpenTelemetry traces in the Aspire dashboard.
- The claiming and recovery logic needs its own integration tests against PostgreSQL.
- In-process notification assumes a single API instance (ADR-010).

## Impact on previous decisions

- Answers the "synchronous versus job-based ingestion" decision required by `06` before block 04.
- Triggers the ADR-007 review: background work arrives, so the API adopts `ServiceDefaults` and OpenTelemetry in 04A.

## Success criterion

- Integration tests show that a version and its job commit atomically, an expired lease is reclaimed, transient failures retry up to the limit, content errors do not retry, and a removed source is never published.

## Future review

Review when job volume, scheduling needs or a second API instance make a library or a broker worth its cost.
