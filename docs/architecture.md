# Architecture

Conductor.NET keeps CQRS contracts independent of the host and of persistence.

```text
application code
  -> ICommandDispatcher / IQueryDispatcher / IMediator
    -> cached typed wrapper (metadata only)
      -> ordered IPipelineBehavior<TRequest, TResponse>
        -> IValidator<TRequest> (optional ValidationBehavior)
          -> single handler

application code
  -> INotificationPublisher / IMediator
    -> cached typed wrapper (metadata only)
      -> zero or more handlers, in registration order
```

## Packages

- `Conductor.Abstractions` has no extra dependencies.
- `Conductor` implements dispatch and pipelines using `IServiceProvider`.
- `Conductor.DependencyInjection` registers services and optionally scans assemblies at startup.
- `Conductor.Testing` and `Conductor.AspNetCore` are optional.

## One request, one handler

A closed command or query type has exactly one handler. Cross-cutting work belongs in pipeline behaviors.

Notifications are a separate one-to-many abstraction. `PublishAsync` invokes every handler registered for the notification's runtime type, in registration order. Zero handlers is success. The first exception stops the remaining handlers. Handlers for a base type or interface are not invoked. Notifications do not enter the command and query pipeline. A domain event is an `INotification` the application publishes explicitly. Conductor does not collect events from aggregates or deliver them across processes.

## Lifetimes

Defaults are scoped for dispatchers, the notification publisher, the mediator, handlers, validators, and behaviors. Singleton dispatchers must not capture scoped services. The wrapper cache stores compiled metadata, never scoped instances.

## Diagnostics

Activity source name: `Conductor`.

Activities:

- `Conductor.Command`
- `Conductor.Query`
- `Conductor.Notification`

Tags: `cqrs.request.type`, `cqrs.request.kind`, `cqrs.handler.type`, `cqrs.handler.count`, `cqrs.outcome`, `cqrs.validation.failure_count`.

`cqrs.handler.count` is set for notifications. `cqrs.handler.type` on a notification activity is the handler that failed. Activities are created only when a listener is present. Request, response, and notification payloads are never recorded.

## Versioning

Semantic versioning:

- Major: breaking public API or behavior
- Minor: backward-compatible features
- Patch: backward-compatible fixes

Breaking changes include renaming public members, changing generic constraints, changing default pipeline order, changing exception types, and changing default lifetimes.
