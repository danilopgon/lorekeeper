# ADR-002 — Cloudflare Access for personal remote access

**Status:** Accepted  
**Date:** 2026-09-21  
**Area:** operations / security / cross-cutting

## Context and problem

Lorekeeper is currently a personal, single-operator installation with multiple campaigns. It may occasionally be shared with explicitly invited trusted people, but accounts, teams, invitations, per-user campaign ownership and application roles are not product requirements for the current release.

Internet exposure still requires an authentication boundary around both the web application and API. Building application-level authentication now would introduce user persistence, login flows, token/session handling and authorization concepts before the product needs them.

The target is therefore the smallest access mechanism that keeps private campaign data off the public Internet without creating a premature multiuser domain.

## Alternatives considered

### Option A — Application-level authentication

Examples include ASP.NET Core Identity or an external identity platform integrated into Lorekeeper.

- Benefits: gives the application first-class user identity and enables per-user ownership, roles and permissions.
- Costs/risks: adds product and implementation scope that the current single-operator release does not need; creates account/session/authorization concepts that would need to be maintained and tested.

### Option B — Cloudflare Access in front of the application

Use a Cloudflare Access self-hosted application with explicit email allow rules and one-time PIN authentication, with the origin reachable through Cloudflare Tunnel where practical.

- Benefits: access is enforced before requests reach Lorekeeper; invited users can be managed by email without adding account models or login UI to the product.
- Costs/risks: access depends on Cloudflare configuration and correct origin protection; this controls who may reach Lorekeeper but does not create application-level ownership or permissions.

### Option C — Shared password or ad-hoc HTTP Basic authentication

- Benefits: minimal setup.
- Costs/risks: shared credentials are difficult to revoke per person, provide weak identity/audit semantics and encourage security logic outside a durable access policy.

## Decision

Use **Cloudflare Access as the external private-access boundary** for the initial personal deployment.

The default human access flow is an explicit email allowlist plus Cloudflare One-Time PIN authentication. Lorekeeper will not implement application accounts, login screens, teams, invitations, roles or per-user campaign ownership for this release.

This decision chooses the access mechanism but does **not** satisfy the Internet-exposure gate by itself. Remote exposure remains blocked until the deployment is configured and verified.

## Proposed design

```text
Internet
   |
Cloudflare Access
(explicit email allowlist + OTP)
   |
Cloudflare Tunnel / protected origin
   |
   +--> Angular web
   |
   +--> ASP.NET Core API
            |
        PostgreSQL
```

### Access policy

- Access is deny-by-default.
- Human users must match an explicit allowed email address.
- One-Time PIN is the initial authentication method.
- Do not create an Allow rule that accepts every valid OTP user.
- Adding or removing an email from the Access policy is the current invitation/revocation mechanism.
- Session duration is configured in Cloudflare, not implemented as a Lorekeeper session model.

### Web, API and origin boundary

- Both the frontend and API must be protected. Protecting only the Angular UI is insufficient.
- Prefer Cloudflare Tunnel so the origin does not need to be publicly routable.
- Enable Access protection/token validation at the tunnel boundary where supported. If traffic can reach an origin without that enforcement, the origin must validate the Access JWT before accepting requests.
- A direct origin hostname, IP address or alternate API hostname must not provide a bypass around Access.
- Database and provider credentials remain private/backend-only and are not exposed through this mechanism.

### Application boundary

- `campaignId` remains data partitioning, not proof of user identity or ownership.
- Do not add `User`, `Account`, `Team`, `Invitation`, `Role` or equivalent product entities for this access mechanism.
- Cloudflare identity is a perimeter access signal, not an application authorization model.
- Do not couple campaign ownership to Cloudflare email/JWT claims without a new product requirement and ADR.
- If non-browser automation is introduced later, define a dedicated machine-access policy rather than weakening the human Access policy.

## Consequences

### Positive

- Private remote access can be added without building an authentication subsystem inside Lorekeeper.
- Individual access can be granted and revoked without shared application credentials.
- The current product/domain remains single-operator and avoids premature multiuser modelling.
- Cloudflare may remain useful as a perimeter layer even if application identity is introduced later.

### Negative / accepted trade-offs

- Deployment security depends on Cloudflare configuration and origin hardening.
- Cloudflare Access answers "may this identity reach Lorekeeper?", not "what may this user do inside Lorekeeper?".
- A future move to campaign sharing, roles or user-owned data will require application-level identity and authorization work.

## Impact on previous decisions

- Resolves the access mechanism intentionally deferred by [ADR-001](ADR-001-personal-multicampaign-and-readiness.md).
- Preserves ADR-001's single-operator, multicampaign boundary.
- Does not change campaign isolation requirements in `03-domain-model.md` or `07-data-security-and-rls.md`.

## Success criterion

The deployment gate is satisfied only when recorded smoke evidence demonstrates all of the following:

- an allowed email can authenticate and use the web application end to end;
- a non-allowed email cannot reach Lorekeeper;
- direct unauthenticated API requests are denied;
- direct-origin access cannot bypass Cloudflare Access;
- frontend and API remain usable through the protected route.

Until that evidence exists, Internet exposure remains **Blocked**.

## Future review

Review this decision when any of the following becomes an approved requirement:

- different users own different campaigns or data;
- campaign sharing needs viewer/editor/owner permissions;
- public signup or self-service invitations are introduced;
- Lorekeeper needs application-level audit identity;
- native clients, external integrations or APIs need durable user authorization semantics.

At that point, define application authentication and server-side authorization explicitly. Cloudflare Access may remain as the outer perimeter but must not substitute for product-level permissions.

## References

- Cloudflare: Publish a self-hosted application to the Internet — https://developers.cloudflare.com/cloudflare-one/access-controls/applications/http-apps/self-hosted-public-app/
- Cloudflare: Access policies — https://developers.cloudflare.com/cloudflare-one/access-controls/policies/
- Cloudflare: One-time PIN login — https://developers.cloudflare.com/cloudflare-one/integrations/identity-providers/one-time-pin/
- Cloudflare: Validate Access JWTs — https://developers.cloudflare.com/cloudflare-one/access-controls/applications/http-apps/authorization-cookie/validating-json/
