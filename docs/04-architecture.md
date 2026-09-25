# 04 — Architecture

## Architectural style

The baseline is an **Angular 22 frontend** backed by an **ASP.NET Core modular monolith**. The block 00 scaffold targets pnpm, Node 24.15+, .NET 10, GitHub Actions and Docker Compose for local PostgreSQL 17. The backend is organised by functional modules and vertical slices, with Clean/Hexagonal boundaries inside each module where they protect real dependencies.

CQRS means commands and queries have distinct use cases and models. Campaign use cases dispatch through MediatR under ADR-003; this does not require a class per line of code or make MediatR mandatory for every module.

## System overview

```text
Angular 22
    ↓ generated OpenAPI client
ASP.NET Core API
    ↓
Functional modules / vertical slices
    ├── Domain
    ├── Application
    └── Infrastructure
          ├── PostgreSQL / pgvector
          └── external and AI providers
```

## Repository layout

```text
apps/web/
├── src/app/
│   ├── core/                 # app-wide infrastructure
│   ├── shared/               # proven reusable UI primitives
│   └── features/             # product capabilities
└── e2e/                      # initial Playwright E2E location

services/api/
├── src/
│   ├── Api/                  # composition root, middleware, endpoints
│   ├── Modules/
│   │   └── [Module]/
│   │       ├── Presentation/        # use-case-oriented HTTP adapters and route composition
│   │       ├── Domain/
│   │       ├── Application/
│   │       │   └── Features/
│   │       │       └── [UseCase]/
│   │       └── Infrastructure/
│   └── SharedKernel/         # genuinely transversal primitives only
└── tests/
    ├── Unit/
    └── Integration/

ai/                           # deferred optional offline AI engineering workbench; not active in block 00
├── experiments/              # retrieval/model comparisons and benchmarks when introduced
├── tools/                    # dataset and analysis utilities when introduced
└── tests/                    # Python-only tooling tests when needed

tests/
├── contracts/
├── e2e/
└── evals/
```

The `ai/` workbench is not an application runtime boundary by default and is not an active block 00 scaffold dependency or check. Add it later only when a corpus/evaluation need exists. Product orchestration, retrieval policy and production contracts remain owned by the ASP.NET Core application. Introduce a separately deployed Python/model service only after a measured need and an ADR justify the operational boundary.

## Block 00 scaffold decisions

- Frontend package manager: pnpm, with committed lockfile once scaffolded.
- Node runtime: Node 24.15+.
- Backend runtime: .NET 10 / ASP.NET Core 10.
- Styling: Tailwind is part of the block 00 frontend scaffold and must map to the durable tokens in `DESIGN.md`.
- CI: GitHub Actions.
- Local database: Docker Compose running PostgreSQL 17; pgvector remains required where semantic retrieval needs it in later blocks.
- Initial E2E location: `apps/web/e2e` for Playwright.
- Commit policy: Conventional Commits enforced by CI plus local hooks using commitlint and Husky/lint-staged or equivalent JavaScript tooling once scaffolded.
- Python/AI evaluation workbench: planned for a later roadmap block when corpus/eval needs exist; not an active block 00 runtime, dependency or CI check.

## Backend module rules

- A module owns its domain rules, use cases, persistence mapping and adapters.
- Modules communicate through explicit contracts, not another module's EF entities.
- Domain and Application do not depend on EF Core, ASP.NET or provider SDKs.
- Infrastructure implements ports defined by the owning module.
- Endpoint adapters live in a module `Presentation/` layer when a module has multiple HTTP use cases. Each use-case-oriented adapter translates HTTP transport DTOs into commands/queries, dispatches through `ISender` where the module adopts MediatR, and maps results into public contracts; a small composition mapper owns shared route-group metadata only.
- Use project-owned ports for time, IDs, storage and providers when the boundary matters; do not wrap every framework API by reflex.

## Pragmatic CQRS

```text
Command → Handler → Domain change → Unit of Work
Query   → Handler → Read model / projection
```

- Commands express intent and enforce invariants.
- Queries can use purpose-built projections and need not hydrate aggregates.
- A feature folder owns request, result, validation, handler and tests.
- Campaign create/list/get messages implement `IRequest<TResponse>` and handlers implement `IRequestHandler<TRequest, TResponse>`; their `Presentation/` adapters inject `ISender`, while `CampaignEndpointComposition` owns the shared route group.
- Add mediator dispatch to another module only through an ADR with a demonstrated benefit. Do not introduce pipeline behaviors, repositories, generic base handlers or wrapper interfaces without a demonstrated need.

## Angular rules

- Standalone components and lazy feature routes.
- Keep Angular artifact suffixes (`*.component.*`, `*.service.ts`, etc.) as configured in `apps/web/angular.json`; see `docs/conventions/angular.md`.
- Signals for local synchronous state; `computed` for derived state.
- `resource`/`httpResource` for remote state when the API and lifecycle fit.
- Zoneless change detection and `OnPush`-compatible patterns.
- Domain components before generic abstractions.
- Generated OpenAPI client at the transport boundary; map DTOs when the UI needs its own model.
- Semantic HTML, Angular ARIA or CDK for behaviour that native HTML cannot provide cleanly.

## Contract flow

```text
ASP.NET endpoint contracts
        ↓ OpenAPI
generated TypeScript client
        ↓ adapter / facade
Angular feature model
```

The generated client is never edited manually. CI fails when a contract change leaves generated code stale.

## AI and retrieval boundary

```text
Application use case
    ↓ project-owned ports
IChatModel / IEmbeddingGenerator / IReranker
    ↓ Infrastructure adapters
Microsoft.Extensions.AI / provider HTTP API / measured local inference adapter
```

Angular never receives provider credentials and never calls providers directly. Retrieval, context construction, validation, cost controls and audit metadata belong in the backend.

A local model implementation may use Hugging Face / SentenceTransformers and PyTorch behind an existing port, especially for reranking experiments. Those libraries are implementation details, not architectural requirements. A local reranker is promoted from experiment to product only when evaluation shows a useful quality gain relative to latency, resource use and operational complexity.

## AI experimentation workbench

Python may be used for offline AI engineering tasks where the ecosystem is materially better suited than .NET, such as:

- embedding and reranker comparisons;
- dataset inspection and generation utilities;
- latency/throughput benchmarks;
- model loading, batching and inference experiments;
- analysis of retrieval/evaluation results.

If introduced, prefer a small modern toolchain such as `uv`, Pydantic and pytest. The workbench must consume the same committed fixtures and contracts as the product where practical; it must not become a parallel source of product truth or duplicate production orchestration.

## Data

- PostgreSQL is the system of record.
- EF Core owns transactional persistence and migrations.
- Full-text search and pgvector may use SQL/Npgsql projections where EF adds friction.
- Migrations are forward-compatible during rollout and reviewed as production code.
- Integration tests run against real PostgreSQL with Testcontainers, not an in-memory substitute.

## Cross-cutting concerns

ASP.NET Problem Details, structured logging, request correlation, OpenTelemetry, health checks, timeouts, resilience, caching and rate limits are configured centrally but applied according to each capability.

## Explicit non-goals

- No microservices without an independently deployable need.
- No Python runtime service solely to adopt AI ecosystem tooling.
- No generic repository over EF Core.
- No service locator or framework types inside Domain.
- No speculative shared library.
- No direct SDK calls from handlers or Angular.
- No training/fine-tuning pipeline without an evaluated product problem that requires it.

## Personal multicampaign deployment

The initial system is single-operator, not multiuser. Campaign boundaries are enforced in application use cases and database relationships; no account or tenant subsystem is required. Scope lexical/vector candidates by campaign before fusion. Include campaign scope in cache keys, jobs, citations and mutations. Any future multiuser design requires a new ownership/authentication decision; `campaignId` alone is not authorization.

Application login is not a scaffold prerequisite. Remote exposure is subject to the access gate in `07-data-security-and-rls.md`.
