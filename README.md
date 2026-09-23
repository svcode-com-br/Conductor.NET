# Conductor.NET

Conductor.NET is a small, host-neutral CQRS and mediator library for .NET. It dispatches commands and queries to exactly one handler, publishes in-process notifications to zero or more handlers, runs ordered pipeline behaviors, and validates requests without taking a dependency on MediatR, Fluent Validator, ASP.NET Core, or Entity Framework Core.

**Status:** `1.0.0-release.1`

## Packages

| Package                         | Purpose                                                                                    |
| ------------------------------- | ------------------------------------------------------------------------------------------ |
| `Conductor.Abstractions`        | Public contracts: commands, queries, notifications, handlers, pipelines, validation        |
| `Conductor`                     | Dispatchers, notification publishing, pipeline execution, validation behavior, diagnostics |
| `Conductor.DependencyInjection` | `AddConductor`, assembly scanning, duplicate detection                                     |
| `Conductor.Testing`             | Fake dispatchers, recording behaviors, scoped test hosts                                   |
| `Conductor.AspNetCore`          | Maps `RequestValidationException` to HTTP 400 ProblemDetails                               |

Target framework: `net10.0` and later.

## Install

```bash
dotnet add package Conductor.Abstractions
dotnet add package Conductor
dotnet add package Conductor.DependencyInjection
```

## Quick start

```csharp
builder.Services.AddConductor(options =>
{
    options.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
});
```

```csharp
public sealed record CreateOrderCommand(string OrderNumber) : ICommand<Guid>;

public sealed class CreateOrderHandler : ICommandHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}
```

```csharp
var orderId = await mediator.SendAsync(
    new CreateOrderCommand("PO-10001"),
    cancellationToken);
```

Manual registration is supported and does not require assembly scanning. That is the recommended path for trimming and native AOT.

## Documentation

- [Getting started](docs/getting-started.md)
- [Architecture](docs/architecture.md)
- [Pipelines](docs/pipelines.md)
- [Validation](docs/validation.md)
- [Security](docs/security.md)
- [Migration](docs/migration.md)

## Design defaults

- Separate `ICommandDispatcher` and `IQueryDispatcher`, plus optional `IMediator`.
- One handler per closed command or query type.
- Zero or more notification handlers, invoked sequentially by `PublishAsync`. A domain event is an `INotification`.
- Multiple ordered behaviors and validators.
- Scoped dispatchers, handlers, validators, and behaviors.
- No automatic retries, transactions, caching, or authorization in the core.
