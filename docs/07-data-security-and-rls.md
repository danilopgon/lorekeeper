# 07 — Data Security and Access Boundary

## Current deployment model

One personal installation for Dani, containing multiple campaigns. Accounts, teams, invitations and multiuser permissions are out of scope. Campaign notes and provider credentials are private. `campaignId` provides data partitioning, not authentication or proof of ownership. [ADR-002](decisions/ADR-002-cloudflare-access-personal-deployment.md) selects Cloudflare Access as the external private-access boundary for remote use; this does not introduce application users or permissions.

## Campaign isolation

Scope all campaign reads/writes, ingestion jobs, lexical/vector candidate searches, caches and citations explicitly. Validate nested relationships and enforce consistent campaign ownership with database constraints. Integration tests must demonstrate isolation with overlapping names and cross-campaign references.

No tenant/RLS subsystem is mandated for this personal release. Future multiuser access requires an explicit ADR, ownership model and authorization checks; query filters alone would not suffice.

## Blocking gate: Internet exposure

Local-only development may proceed without application login. [ADR-002](decisions/ADR-002-cloudflare-access-personal-deployment.md) resolves the access-mechanism decision: the initial remote deployment uses Cloudflare Access with an explicit email allowlist and One-Time PIN authentication.

Selecting the mechanism does **not** unblock Internet exposure. The gate is satisfied only when:

- Cloudflare Access is configured for every public web/API hostname with deny-by-default policies and explicit allowed email addresses.
- Both UI and API are protected; direct backend/origin access cannot bypass Access.
- Cloudflare Tunnel is preferred for origin connectivity. Access token validation is enforced at the tunnel boundary where supported, or by the origin if traffic can otherwise reach it.
- An unauthorised email is denied, an authorised email completes the end-to-end flow, and direct unauthenticated API/origin requests fail.
- Database and secrets are not publicly exposed; provider credentials remain backend-only.
- Deployment verification evidence is recorded in `09-delivery.md`.

Do not add application accounts, roles or campaign ownership merely to satisfy this gate. Cloudflare identity controls who may reach Lorekeeper; it is not a product authorization model.

Until the configuration and smoke evidence exist, remote exposure is **Blocked**, regardless of roadmap progress. This does not block local ingestion/retrieval work. Public anonymous access to real campaign data is not an acceptable interim deployment.

## Private data in ingestion and evaluation

Decided on 2026-11-11 for blocks 04A and 03. Notion (04B) and any hosted AI provider still need their own decision before they receive private notes.

- **What leaves the machine:** nothing in 04A. Block 03 develops against a local Ollama, so embedding and chat calls stay on the operator's machine. Sending note content to a hosted provider such as NaN requires an explicit decision before block 06 (`05`).
- **Logs and telemetry:** record IDs, sizes, hashes, counts, durations, model names, token counts and error codes. Never record note content, source titles, file names, prompts or completions.
- **Removal:** removing a source deletes its versions, chunks and jobs. A tombstone keeps only `id`, `campaignId`, `title` and `removedAt` for later citations. Superseded versions are deleted after a successful update (`03`).
- **Fixtures:** committed tests and evaluation fixtures are synthetic.
- **Local evaluation with real notes:** allowed only from a git-ignored folder, against local providers only, recording aggregate metrics and never content.

## Secrets and resilience

Keep credentials outside Git; examples contain placeholders only. Do not log private prompts or tokens by default. Define request-size limits, provider budgets, timeouts and abuse controls at the corresponding implementation/deployment gates. Source upload limits are in `06`; AI provider timeouts and retries are in `05`.
