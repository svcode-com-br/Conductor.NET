# Getting started

## Installation

```bash
dotnet add package Conductor.DependencyInjection
```

`Conductor.DependencyInjection` references the runtime and abstractions packages.

## Registration

Assembly scanning:

```csharp
services.AddConductor(options =>
{
    options.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
});
```

Manual registration (required-friendly for trimming and native AOT):

```csharp
services.AddConductor();

services.AddScoped<
    ICommandHandler<CreateOrderCommand, Guid>,
    CreateOrderCommandHandler>();

services.AddScoped<
    IQueryHandler<GetOrderQuery, OrderDto>,
    GetOrderQueryHandler>();

services.AddScoped<
    IValidator<CreateOrderCommand>,
    CreateOrderCommandValidator>();
```

Scanning is optional. `AddConductor` does not require an assembly list.

## Command

```csharp
public sealed record CreateOrderCommand(string OrderNumber) : ICommand<Guid>;

public sealed class CreateOrderHandler
    : ICommandHandler<CreateOrderCommand, Guid>
{
    public Task<Guid> HandleAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}
```

Commands that do not return business data use `ICommand<Unit>`.

## Query

```csharp
public sealed record GetOrderQuery(Guid Id) : IQuery<OrderDto>;
```

Queries must not intentionally change application state.

## Validator

```csharp
public sealed class CreateOrderValidator : IValidator<CreateOrderCommand>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();
        if (string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            failures.Add(new ValidationFailure(
                nameof(request.OrderNumber),
                "OrderNumber.Required",
                "OrderNumber is required."));
        }

        return ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>(failures);
    }
}
```

## Dispatch

```csharp
var orderId = await commandDispatcher.SendAsync(
    new CreateOrderCommand("PO-10001"),
    cancellationToken);

var order = await queryDispatcher.QueryAsync(
    new GetOrderQuery(orderId),
    cancellationToken);
```

`IMediator` exposes both methods when a single facade is preferred.

## Exceptions and cancellation

- Missing handler: `HandlerNotFoundException`
- Duplicate closed handlers at registration: `DuplicateHandlerException`
- Validation failures: `RequestValidationException`
- Cancellation: `OperationCanceledException` / `TaskCanceledException`

The library does not convert cancellation into validation or business errors.

## Testing

Use `Conductor.Testing` for fake dispatchers, recording behaviors, and a scope-validated `ConductorTestHost`. Core library tests should not require a web host or database.

## Blazor

Interactive Server components may inject dispatchers. WebAssembly components must call secured server endpoints. Do not put server-only handlers in a client project. See [security.md](security.md).
