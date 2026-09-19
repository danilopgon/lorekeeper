# Frontend Conventions

## Angular async boundaries

- Feature API adapters expose `Promise<T>` methods for one-shot HTTP request/response operations.
- Route and page reads consume adapter promises through Angular `resource`.
- UI commands and mutations may wrap adapter promises with RxJS `from(...)` in the page/component layer.
- Command flows use extracted success/error handlers, `finalize(...)`, and `takeUntilDestroyed(...)` when subscribing.
- API adapters do not expose Observables unless a future use case is genuinely streaming or multi-emission.

This keeps transport mapping simple while allowing command handlers to use familiar RxJS orchestration in UI code.
