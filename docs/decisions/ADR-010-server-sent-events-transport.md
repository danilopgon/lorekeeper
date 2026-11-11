# ADR-010 — Server-Sent Events as the server-to-browser transport

**Status:** Accepted
**Date:** 2026-11-11
**Area:** cross-cutting

## Context and problem

Block 04A shows per-document ingestion progress from background jobs (ADR-009). Block 06 streams generated answers. Issue #21 asked whether streaming should use SSE or gRPC plus SSE. The maintainer decided on 2026-11-11 to settle the transport now for both, instead of polling in 04A and choosing again in 06.

## Alternatives considered

### Option A — Polling

- Benefits: reuses `httpResource`; nothing new on the server.
- Costs/risks: latency equals the polling interval; 06 still needs a streaming transport.

### Option B — SSE for 04A only, chat transport decided in 06

- Benefits: smallest commitment.
- Costs/risks: two decisions for the same need; #21 stays open.

### Option C — gRPC (with gRPC-Web in the browser) plus SSE

- Benefits: typed streaming contracts.
- Costs/risks: browsers need gRPC-Web and proto tooling; no benefit for a single same-origin Angular client.

### Option D — SSE for every server-to-browser stream

- Benefits: one transport, one client helper and one reconnection rule; OpenAI-compatible providers already stream as SSE; works over plain HTTP behind Cloudflare Access.
- Costs/risks: `EventSource` only issues GET requests, so a chat question sent by POST must be read with `fetch` and a streamed body.

## Decision

Use **Option D**. Close #21 without a gRPC spike.

## Proposed design

- **State first, then events.** A normal GET returns the current state. The client then opens the event stream. On every (re)connection the client fetches the state again, so missed events never leave the view stale.
- **04A:** `GET /api/campaigns/{campaignId}/sources/events` streams source changes for one campaign (contract in `06`). Background workers publish to an in-process channel that the endpoint fans out to connected clients. A comment heartbeat keeps idle connections open.
- **06:** the chat endpoint accepts the question by POST and responds with `text/event-stream`; Angular reads it through `fetch`. Its event schema is specified before 06.
- Events carry `campaignId`; the client discards events for any campaign other than the one in the URL.

## Consequences

### Positive

- One streaming mechanism from 04A to 06.
- No new dependency on the server (ASP.NET Core writes `text/event-stream` directly) or in the browser.

### Negative / accepted trade-offs

- The in-process fan-out assumes one API instance. A second instance would need PostgreSQL `LISTEN/NOTIFY` or a broker.
- Streaming responses must not be buffered by any proxy in front of the API; verify with the Angular dev proxy and, later, Cloudflare.

## Impact on previous decisions

- Resolves #21 and the "job polling/event transport" decision in `06`.
- ADR-002 is unchanged.

## Success criterion

- The Sources view reflects job state changes without polling, recovers after a dropped connection by refetching state, and never shows another campaign's events.

## Future review

Review if the API runs as more than one instance or if a client other than the Angular app needs the streams.
