# Frontend Conventions

## Angular async boundaries

- Feature API adapters expose `Promise<T>` methods for one-shot HTTP request/response operations.
- Route and page reads consume adapter promises through Angular `resource`.
- UI commands and mutations may wrap adapter promises with RxJS `from(...)` in the page/component layer.
- Command flows use extracted success/error handlers, `finalize(...)`, and `takeUntilDestroyed(...)` when subscribing.
- API adapters do not expose Observables unless a future use case is genuinely streaming or multi-emission.

This keeps transport mapping simple while allowing command handlers to use familiar RxJS orchestration in UI code.

## Styling

- Use Tailwind utility classes for feature UI by default.
- Keep durable visual decisions in `DESIGN.md` and shared frontend conventions, not in one-off component CSS.
- Component CSS is an exception for genuinely reusable selectors, browser quirks, or styles that cannot be expressed clearly with Tailwind utilities. Document the reason near the component or in the relevant convention when introducing it.
- Routable UI still keeps templates in separate `.html` files; using Tailwind in those templates is preferred over page-local CSS selectors.

## API origins and environments

- Frontend API adapters build URLs from `environment.apiBaseUrl`.
- The default value is an empty string, so production assumes the API is available under the same origin unless a deployment-specific build replaces it.
- Local development keeps `apiBaseUrl` empty and uses the Angular dev-server proxy (`proxy.conf.json`) for `/api` requests.
- Internet-facing deployment is blocked until the deployment slice chooses and verifies the API origin, CORS policy, private-access/authentication boundary, and OWASP-aligned hardening. Do not weaken CORS or expose the API publicly as a convenience for frontend wiring.
