# Security Policy

## Supported versions

Security updates are provided for the latest released minor version of Conductor.NET.

## Reporting a vulnerability

Please do not open a public issue for security problems. Email the maintainers with:

- A description of the issue and its impact
- Reproduction steps or a proof of concept that does not include secrets
- Affected package versions and target frameworks

Do not include credentials, connection strings, personal data, or production payloads in the report.

## Library security expectations

- Conductor.NET does not log request or response bodies by default.
- Authorization, tenant isolation, caching, retries, and transactions are consumer-defined.
- Assembly scanning uses only assemblies the consumer supplies.
- Validation failures must not include secrets in exception messages or `ValidationFailure` values.
