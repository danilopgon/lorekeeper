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

## Decisions before real data ingestion

Document what campaign content is sent to Notion/AI providers, retention/removal policy and log redaction before ingesting private notes. Synthetic fixtures may be used while these decisions are open.

## Secrets and resilience

Keep credentials outside Git; examples contain placeholders only. Do not log private prompts or tokens by default. Define request-size limits, provider budgets, timeouts and abuse controls at the corresponding implementation/deployment gates.
