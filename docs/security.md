# Security

Conductor.NET does not implement authentication or authorization. Server-side dispatch must enforce access control. UI checks are only a usability feature.

## Rules

- Do not log request or response bodies by default; they may contain secrets or personal data.
- Do not trust site, tenant, or plant identifiers from the browser without an access check.
- Request-level permissions do not replace resource-level authorization after data is loaded.
- Cache keys must isolate tenants and security scopes.
- Do not retry non-idempotent commands automatically.
- Idempotency stores must be durable when multiple instances can process the same command.
- Request types shared with browser clients must contain no secrets.
- Assembly scanning uses only explicitly supplied assemblies.

## Blazor

Interactive Server may inject dispatchers. WebAssembly cannot use handlers that depend on server repositories, secrets, or database contexts. Call secured HTTP endpoints and authorize again on the server.

## Reporting

See [SECURITY.md](../SECURITY.md).
