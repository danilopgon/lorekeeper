# AGENTS.md

You are working on **Lorekeeper**, an AI-enabled product built with Angular and ASP.NET Core.

Before implementing product features, read `PRODUCT.md`, `DESIGN.md` and `docs/README.md`.

---

## Project Summary

Lorekeeper helps its operator recover campaign knowledge from notes with source-backed answers.

The GM retains control over campaign canon; retrieved preparation is not automatically an event that happened.

---

## Stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 22, TypeScript, standalone APIs, signals, zoneless, Tailwind |
| Backend | ASP.NET Core/.NET 10, C#, vertical slices, pragmatic CQRS |
| Database | PostgreSQL 17, EF Core, pgvector where semantic retrieval is required |
| AI | Microsoft.Extensions.AI behind project-owned ports; OpenAI-compatible providers |
| Contracts | OpenAPI with a generated TypeScript client |
| Frontend QA | Vitest, Angular Testing Library, Playwright |
| Backend QA | xUnit, FluentAssertions, NSubstitute, WebApplicationFactory, Testcontainers |

---

## Repository Structure

```text
Lorekeeper/
├── apps/web/            # Angular 22 frontend
├── services/api/        # ASP.NET Core modular monolith
├── tests/               # Cross-system contract, E2E and AI evaluation suites
├── docs/                # SDD working documentation
├── PRODUCT.md           # Product source of truth
├── DESIGN.md            # Design system and UX rules
├── AGENTS.md            # This file
└── CLAUDE.md            # Imports AGENTS.md
```

---

## Command Reference

| Command | What it does |
| --- | --- |
| `pnpm install --frozen-lockfile` | Install frontend dependencies from the lockfile |
| `pnpm run start` | Start Angular locally |
| `pnpm run build` | Build the Angular application |
| `pnpm run lint` | Run Angular ESLint |
| `pnpm run test` | Run frontend unit/component tests with Vitest |
| `pnpm run e2e` | Run Playwright end-to-end tests |
| `dotnet restore services/api/Lorekeeper.slnx` | Restore backend dependencies |
| `dotnet build services/api/Lorekeeper.slnx --no-restore --configuration Release` | Build .NET with analyzers |
| `dotnet test services/api/Lorekeeper.slnx --no-build --configuration Release` | Run .NET unit and integration tests |
| `dotnet format services/api/Lorekeeper.slnx --verify-no-changes` | Verify .NET formatting |

---

## Quality Expectations

- All code must pass the applicable typecheck, lint and test commands.
- Do not introduce secrets, credentials or real personal data.
- Prefer small, focused changes. Each change should tell one clear story.
- Preserve existing behavior unless the current specification changes it explicitly.
- Update affected documentation in the same change.

---

## Testing Expectations

- Test behavior at the cheapest layer that proves it.
- Cover the happy path, expected failures and relevant boundaries.
- Add integration tests at persistence and provider boundaries.
- Keep E2E tests for critical user flows.
- Do not declare work complete with a broken or skipped relevant check.

---

## Documentation Routing

| Task | Read first |
| --- | --- |
| Product understanding | `PRODUCT.md`, `docs/00-product-brief.md` |
| Current scope and boundaries | `docs/01-scope.md` |
| User stories and acceptance criteria | `docs/02-requirements-and-acceptance.md` |
| Entities, concepts and domain rules | `docs/03-domain-model.md` |
| Frontend/backend architecture | `docs/04-architecture.md` |
| LLM, retrieval, prompts and evals | `docs/05-ai-system.md` |
| API and integration contracts | `docs/06-api-contracts.md` |
| Authentication, ownership and data security | `docs/07-data-security-and-rls.md` |
| Testing, CI and quality gates | `docs/08-quality-strategy.md` |
| Delivery, environments and deployment | `docs/09-delivery.md` |
| Implementation phases and current status | `docs/10-roadmap.md` |
| Deferred work and intentional non-goals | `docs/11-backlog.md` |
| Architecture decisions | `docs/decisions/README.md` and related ADRs |
| Documentation conventions | `docs/conventions/documentation.md` |
| UI/UX visual direction | `DESIGN.md` and the nearest shipped screen |

Read only the relevant route after the core documents.

---

## Rules

### PRODUCT.md is the product source of truth

Product principles, scope boundaries, flows and required states live in `PRODUCT.md`. Do not change product behavior from an implementation document alone.

### DESIGN.md is the durable design reference

Use its tokens, typography, components, states and motion rules. Do not reinterpret the visual direction into a generic design system.

### Do not implement outside the current specification

Only implement behavior explicitly described by the current scope, requirements and active roadmap block. Do not scaffold deferred ideas “for later”.

### Durable decisions require an ADR

Do not silently replace architecture, providers, public contracts or ownership boundaries. Create or supersede an ADR.

### CQRS is a boundary, not a ceremony generator

Separate commands from queries and give each use case a clear handler. Do not add MediatR, repositories, base handlers or wrapper interfaces unless they solve a demonstrated boundary.

### AI providers stay behind project-owned ports

Angular never calls an AI provider directly. The application layer depends on project-owned chat, embedding and reranking ports. Provider SDKs remain in Infrastructure.

### Validate external data at the boundary

Never trust requests, provider responses, generated output or persisted JSON directly. Parse and validate them before they enter the domain.

### Ownership is not optional

Every operation on private data must verify authorization server-side. Hiding UI controls is not authorization.

### Prefer self-documenting code

Reach for a better name, smaller function, typed boundary or test name before a narrative comment.


## SDD readiness is a blocking gate

Before starting any implementation block, apply the Definition of Ready in `docs/08-quality-strategy.md` and its entry criteria in `docs/10-roadmap.md`. An unresolved decision required by that block means **Blocked**: record the question, affected document and unblock condition. Do not invent product decisions from placeholders. Independent ready work may proceed. Update specifications before implementation when behaviour changes.

The current release is a personal, single-operator installation supporting multiple campaigns. All campaign data and retrieval are explicitly scoped by `campaignId`. This is data isolation, not authentication. Do not scaffold accounts, teams, invitations or multiuser permissions. Local development may proceed without application login; exposing UI or API on the Internet is blocked until application authentication or private-access enforcement is selected and verified. See `docs/07-data-security-and-rls.md`.

Command tables must reflect verified scaffold commands. A placeholder never counts as a completed specification. Conventional Commits are enforced by CI plus local hooks.

## Workspace and ingestion routing

For campaign navigation, Chat, Sources, uploads, pasted text, Notion import or source lifecycle, read `docs/12-workspace-and-ingestion-ux.md` alongside PRODUCT.md and the relevant contracts/domain/roadmap routes. It is an accepted first approach with unresolved detailed decisions, not a waiver of SDD readiness gates.

## Technical interview preparation

Activate interview mode only when the user explicitly requests interview preparation, a mock interview or a technical walkthrough for interview preparation. In that mode, follow `docs/conventions/interview-preparation.md`.

For ordinary code explanations or technical walkthroughs, explain the implementation directly without activating interview mode or requiring the user to answer questions.

Use actual repository code to rehearse implementation reasoning, architectural trade-offs, debugging and requirement changes across Angular and .NET. Keep this mode optional and separate from normal delivery.
