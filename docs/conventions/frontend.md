# Frontend Conventions

## Angular async boundaries

- Feature API adapters expose `Promise<T>` methods for one-shot HTTP request/response operations.
- Route and page reads consume adapter promises through Angular `resource`.
- UI commands and mutations may wrap adapter promises with RxJS `from(...)` in the page/component layer.
- Command flows use extracted success/error handlers, `finalize(...)`, and `takeUntilDestroyed(...)` when subscribing.
- API adapters do not expose Observables unless a future use case is genuinely streaming or multi-emission.

This keeps transport mapping simple while allowing command handlers to use familiar RxJS orchestration in UI code.

## API origins and environments

- Frontend API adapters build URLs from `environment.apiBaseUrl`.
- The default value is an empty string, so production assumes the API is available under the same origin unless a deployment-specific build replaces it.
- Local development keeps `apiBaseUrl` empty and uses the Angular dev-server proxy (`proxy.conf.json`) for `/api` requests.
- Internet-facing deployment is blocked until the deployment slice chooses and verifies the API origin, CORS policy, private-access/authentication boundary, and OWASP-aligned hardening. Do not weaken CORS or expose the API publicly as a convenience for frontend wiring.
