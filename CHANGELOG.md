# Changelog

All notable changes to Conductor.NET are documented in this file.

## Unreleased

### Added

- In-process notification publishing through `INotification`, `INotificationHandler<T>`, `INotificationPublisher.PublishAsync`, and `IMediator.PublishAsync`. Handlers for a notification's runtime type run sequentially in registration order. Zero handlers is success, and the first exception stops the rest.

## 1.0.0-alpha.1 - 2026-09-22

### Changed

- Packages now target `net10.0` only. .NET 8 is not supported.

### Added

- Command and query abstractions, handlers, dispatchers, and `IMediator` facade.
- Cached typed dispatch wrappers, ordered pipeline behaviors, and built-in validation.
- `AddConductor` registration, explicit assembly scanning, and duplicate-handler detection.
- `Conductor.Testing` helpers and `Conductor.AspNetCore` ProblemDetails mapping.
- Console, Web API, and Blazor Server samples.
- Initial benchmarks and CI quality gates.
