# Pipelines

Behaviors wrap the handler. The first registered behavior is the outermost and executes first.

## Contract

A behavior may:

- run work before `next`
- run work after `next`
- short-circuit without calling `next`
- throw

A behavior must not call `next` more than once and must not dispose dependencies it does not own.

## Registration order

```csharp
services.AddConductor(options =>
{
    options.AddBehavior(typeof(LoggingBehavior<,>));
    options.AddBehavior(typeof(AuthorizationBehavior<,>));
});
```

If `AddValidationBehavior` is `true` (the default), `ValidationBehavior<,>` is registered after option behaviors, so it runs closer to the handler than logging registered via `AddBehavior`.

Behaviors added to `IServiceCollection` after `AddConductor` run even more inward.

## Recommended command order

```text
diagnostics / logging
  -> validation
    -> authorization
      -> idempotency
        -> transaction
          -> command handler
```

## Recommended query order

```text
diagnostics / logging
  -> validation
    -> authorization
      -> cache
        -> query handler
```

Logging should normally wrap the full execution so validation and authorization failures are visible. Cache lookup must occur after access checks unless entries are isolated by user, tenant, and permission.

## Consumer-defined behaviors

Authorization, transactions, caching, and idempotency are not implemented in the core package. Define markers and services in the consuming application, for example `IRequirePermission` or `ITransactionalCommand`.

Retries must not be the default. A retry behavior, if introduced by the consumer, must require an explicit marker and must document interaction with transactions and idempotency.
