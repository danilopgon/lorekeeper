# Documentation and Comment Conventions

## Documentation

- Put the conclusion or decision first.
- Separate current state, target state and transition plan.
- State non-goals when they prevent plausible scope creep.
- Prefer concrete examples, contracts and file trees over vague principles.
- Keep product truth in `PRODUCT.md`, visual truth in `DESIGN.md`, technical shape in `04-architecture.md`, AI behavior in `05-ai-system.md` and decisions in ADRs.
- Update or supersede stale statements. Do not leave contradictory directions active.
- Do not duplicate a rule across files; link to its source of truth.

## Evolving conventions

- Add a convention when the same ambiguity appears more than once or when a tooling decision needs a durable rule for future agents.
- Record conventions in `docs/conventions/` when they are repository-wide, in the owning topic document when they are scoped, or in an ADR when they change architecture, contracts, providers, ownership or delivery boundaries.
- Update conventions in the same change that introduces or enforces the practice; do not rely on chat context or commit history as the only source of truth.
- If a convention is planned but not yet enforced, state the enforcement point and avoid claiming it is verified.

## Commit conventions

- Use Conventional Commits after block 00 scaffold introduces enforcement: `type(scope): subject`.
- Initial types: `feat`, `fix`, `docs`, `test`, `refactor`, `build`, `ci`, `chore`.
- Initial scopes: `docs`, `web`, `api`, `contracts`, `e2e`, `repo`.
- CI and local hooks are the enforcement point once scaffolded; commits before that point may not follow the convention.

## Code comments

Default to no comment. Prefer a better name, smaller function, typed boundary or descriptive test.

A comment is justified only when the constraint cannot be expressed through code, a competent reader might otherwise break it, and it can be stated briefly.

Do not restate code, narrate steps, mark decorative sections or preserve history already available in git.

## Public API documentation

Document only public surfaces where documentation is consumed by people, generated contracts or another module. Do not add docstrings merely because a member is technically public for framework or template access.

For Angular/TypeScript, JSDoc is reserved for exported contract surfaces, shared adapters, non-obvious invariants or APIs intended for reuse outside their local feature. Internal components, tests, local helpers and obvious methods remain self-documenting by default.

For ASP.NET Core, backend HTTP contract documentation belongs primarily in OpenAPI endpoint metadata: summaries, descriptions, response types, status codes, error codes and examples when useful. Do not require blanket XML comments for application/domain/internal vertical-slice code. Use XML documentation only for shared library surfaces or non-obvious constraints that OpenAPI metadata cannot express.
