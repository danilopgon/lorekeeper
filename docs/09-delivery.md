# 09 — Delivery and Operations

## Environments

| Environment | Purpose | Data policy |
| --- | --- | --- |
| Local | Development and automated integration tests with Docker Compose PostgreSQL 17 | Seeded/non-sensitive |
| Staging | Contract, migration and smoke validation | Synthetic or protected |
| Production | Real users | Least privilege and audited access |

## Configuration

Block 00 local development choices are pnpm, Node 24.15+, .NET 10 and Docker Compose PostgreSQL 17. The local application remains local-only with no application login. [ADR-002](decisions/ADR-002-cloudflare-access-personal-deployment.md) selects Cloudflare Access for remote access, but Internet exposure stays blocked until that boundary is configured and verified. Python is not part of the block 00 runtime or delivery baseline.

Document variable names and purpose in `.env.example` or platform configuration. Never commit real values.

| Variable | Used by | Purpose |
| --- | --- | --- |
| `[NAME]` | Web/API | `[purpose]` |

## Deployment shape

Baseline application shape:

```text
Angular static/SSR host
        ↓ HTTPS
ASP.NET Core container
        ↓
PostgreSQL + pgvector
        ↓ outbound only
AI and external providers
```

Initial remote-access boundary when deployment is enabled:

```text
Internet
   ↓
Cloudflare Access
(explicit email allowlist + OTP)
   ↓
Cloudflare Tunnel / protected origin
   ├─ Angular web
   └─ ASP.NET Core API
```

Cloudflare Tunnel is the preferred origin path so the deployment does not require a publicly reachable origin. If an origin can be reached outside the protected route, it must validate the Access token and reject bypass traffic. See ADR-002.

A separate Python/model service is not part of the baseline. Add one only when a measured local inference requirement cannot be served cleanly inside the existing deployment and the extra operational boundary is justified by an ADR.

## Pipeline

CI runs on GitHub Actions. The block 00 scaffold establishes pnpm frontend installation, .NET 10 backend restore/build/test, Playwright smoke coverage and local database expectations around Docker Compose PostgreSQL 17.

## Branch and pull request policy

After the block 00 scaffold is committed, protect `main` and use feature branches plus pull requests for subsequent slices. Required checks should include the GitHub Actions workflow and Conventional Commit validation before merging.

1. Restore/install from lockfiles.
2. Run format, lint, typecheck, build and unit tests.
3. Generate OpenAPI and verify the TypeScript client.
4. Run integration tests against Testcontainers.
5. Run deterministic AI evals when introduced and critical Playwright flows from `apps/web/e2e`.
6. Run applicable reproducible AI-workbench checks only after that tooling exists and owns a changed capability.
7. Build immutable frontend/backend artifacts.
8. Apply forward-compatible migrations.
9. Deploy, verify health/readiness and execute smoke tests.

## Database changes

- Expand before contract: add compatible schema first, migrate consumers, remove old shape later.
- Do not couple an irreversible migration to an unverified application rollout.
- pgvector extension and index strategy are provisioned explicitly.
- Test migrations from the last production schema, not only against an empty database.

## Observability

OpenTelemetry is the system-wide baseline:

- traces across API, database and provider calls;
- structured logs with request/correlation ID;
- health checks that distinguish liveness from readiness;
- domain metrics, retrieval latency, provider usage, validation failures and fallback rate;
- actionable alerts that point to a runbook.

An AI-focused observability platform such as Langfuse may complement this baseline for model/prompt traces, retrieval context, token/cost analysis and evaluation views. Do not duplicate sensitive content into a second telemetry store by default. Define redaction, retention and environment policy before enabling raw prompt/context capture.

If local inference is promoted to production, additionally observe at least:

- model/version and hardware/device;
- batch size and queue time when applicable;
- inference latency and error rate;
- memory/resource pressure;
- fallback behaviour to another implementation/provider.

## AI experiment and model artefacts

Offline Python tooling, Hugging Face models and benchmark outputs are development/evaluation concerns unless explicitly promoted to the runtime. Pin model identity/revision for reproducible experiments. Do not let a floating model download silently change a blocking baseline.

Any promoted local model must define:

- model source, revision and licence review;
- immutable or reproducibly fetched artefact strategy;
- expected storage/memory footprint;
- startup/readiness behaviour;
- rollback/fallback path;
- benchmark evidence supporting the added operational cost.

## Rollback

Define the last known-good artifact, database compatibility window, feature-flag behaviour and how provider/config changes are reversed.

For AI changes, rollback must include model/provider configuration and any reranker/embedding implementation whose output can affect retrieval behaviour. Preserve compatibility with stored embedding dimensions when changing embedding models; reindexing must be explicit, observable and reversible at the application level where practical.

## Initial personal deployment and readiness

The initial installation serves one operator and several campaigns. Local-only development is permitted without application login. Block 00 does not scaffold accounts, teams, invitations or a Python runtime.

The access mechanism is **decided but not yet deployed**: ADR-002 selects Cloudflare Access with explicit allowed emails and One-Time PIN authentication. Hosting, concrete hostnames, tunnel/origin configuration, backups, operational limits and rollback remain deployment-time decisions.

Before any Internet exposure:

- configure Access for both frontend and API;
- prefer Cloudflare Tunnel and prevent direct-origin bypass;
- verify an allowed email can use the complete flow;
- verify a non-allowed email, direct API call and direct-origin request are denied;
- record host, network/origin restrictions, secrets handling, storage/backups and smoke evidence here.

Until that evidence exists, keep the deployment local-only. This deployment gate does not block local product development.
