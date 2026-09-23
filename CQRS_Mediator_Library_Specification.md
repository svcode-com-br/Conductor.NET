# CQRS and Mediator Library Specification for .NET

**Status:** Draft for implementation  
**Audience:** Library maintainers, application architects, .NET developers, reviewers, and consuming teams  
**Purpose:** Define a reusable, framework-neutral .NET library that adds CQRS, mediator-style dispatch, handlers, pipelines, and validation to multiple applications without depending on MediatR.

---

## 1. Executive Summary

This specification defines a reusable .NET library that provides:

- Separate command and query abstractions.
- Strongly typed command and query handlers.
- Command and query dispatchers.
- A mediator facade for teams that prefer one entry point.
- Ordered pipeline behaviors.
- Asynchronous validation.
- Authorization and execution-context hooks.
- Transaction, logging, performance, caching, and idempotency extension points.
- Dependency-injection registration and assembly scanning.
- Predictable exception and cancellation behavior.
- Test utilities and conformance tests.
- NuGet packaging, versioning, diagnostics, and compatibility requirements.

The library must remain small and infrastructure-agnostic. It must not depend on ASP.NET Core, Entity Framework Core, Blazor, a database provider, an identity provider, or a logging implementation. Integrations with those technologies belong in optional companion packages or in consuming applications.

The library is intended for Web APIs, Blazor Server, Blazor Web Apps, worker services, console apps using the Generic Host, and other .NET applications that use `Microsoft.Extensions.DependencyInjection`.

> The term **CQRS** is used throughout this document. Commands express intent to change state. Queries retrieve data and must not intentionally change application state.

---

## 2. Goals

### 2.1 Functional goals

The library shall:

1. Dispatch exactly one handler for each command or query.
2. Support commands and queries returning any response type.
3. Provide `Unit` for operations that do not return business data.
4. Support zero or more ordered pipeline behaviors.
5. Carry `CancellationToken` from caller through every behavior and handler.
6. Support asynchronous validators without requiring FluentValidation.
7. Support explicit, deterministic registration of handlers, validators, and behaviors.
8. Offer optional reflection-based assembly scanning during startup only.
9. Provide useful errors when a handler is missing or registration is ambiguous.
10. Work with built-in .NET dependency injection.
11. Be testable without a web host, database, or external service.
12. Allow consumer-defined security, transaction, cache, audit, and idempotency behaviors.

### 2.2 Quality goals

The implementation shall be:

- **Small:** avoid recreating every feature of a large mediator framework.
- **Predictable:** no hidden retries, transactions, or exception conversion.
- **Strongly typed:** preserve compile-time request and response relationships.
- **Dependency-light:** keep the core package minimal.
- **Host-neutral:** no ASP.NET Core or Blazor dependency in the core runtime.
- **Observable:** expose stable logging and diagnostic hooks.
- **Safe:** validate configuration and fail early when possible.
- **Compatible:** support multiple target frameworks according to the package policy.

---

## 3. Non-Goals

The initial version shall not:

- Act as a distributed message bus.
- Guarantee delivery across processes.
- Replace a queue, event broker, or workflow engine.
- Implement sagas or process managers.
- Automatically persist commands.
- Automatically retry failed requests.
- Automatically create database transactions.
- Discover handlers on every dispatch.
- Serialize requests or responses.
- expose ASP.NET authorization types in the core package.
- Include Entity Framework Core in the core package.
- Deliver notifications across processes, persist them, or retry them.
- Support multiple command or query handlers for the same closed request type.

In-process notifications use a separate one-to-many abstraction (`INotification`) in the core packages. A domain event is an application notification published explicitly after a command succeeds. The library does not collect events from aggregates.

---

## 4. Design Principles

### 4.1 Dependency inversion

Consumers depend on interfaces. Runtime implementations use the application's service provider to resolve handlers and pipeline components.

### 4.2 Explicit CQRS separation

Commands and queries use different marker interfaces, handler interfaces, and dispatcher interfaces. This enables distinct policies:

- Commands may use transactions, idempotency, and auditing.
- Queries may use caching and read-performance instrumentation.

### 4.3 One request, one handler

A command or query has exactly one primary handler. Cross-cutting operations belong in pipeline behaviors, not additional handlers.

### 4.4 Pipelines for cross-cutting concerns

Behaviors wrap the handler and may execute logic before and after the next delegate. A behavior must call `next()` exactly once unless it intentionally short-circuits.

### 4.5 No service-locator leakage

Application handlers must receive dependencies through constructors. `IServiceProvider` is an internal runtime mechanism and must not be exposed in command or query handlers.

### 4.6 Fail fast at startup when practical

Assembly registration should detect:

- Duplicate closed handlers.
- Invalid handler implementations.
- Abstract or open handler classes that cannot be activated.
- Invalid options.

### 4.7 Consumer ownership of business policy

The library provides extension points, not organization-specific permissions, site rules, transaction semantics, or cache-key policies.

---

## 5. Recommended Repository and Package Structure

```text
CqrsKit/
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── CqrsKit.sln
├── README.md
├── CHANGELOG.md
├── LICENSE
├── SECURITY.md
├── docs/
│   ├── architecture.md
│   ├── getting-started.md
│   ├── pipelines.md
│   ├── validation.md
│   ├── security.md
│   └── migration.md
├── src/
│   ├── CqrsKit.Abstractions/
│   ├── CqrsKit/
│   ├── CqrsKit.DependencyInjection/
│   └── CqrsKit.Testing/
├── tests/
│   ├── CqrsKit.Abstractions.Tests/
│   ├── CqrsKit.Tests/
│   ├── CqrsKit.DependencyInjection.Tests/
│   ├── CqrsKit.ArchitectureTests/
│   └── CqrsKit.IntegrationTests/
└── samples/
    ├── CqrsKit.Sample.Console/
    ├── CqrsKit.Sample.WebApi/
    └── CqrsKit.Sample.Blazor/
```

### 5.1 Package responsibilities

#### `CqrsKit.Abstractions`

Contains only stable public contracts and small value types:

- `ICommand<TResponse>`
- `IQuery<TResponse>`
- `INotification`
- Handler interfaces, including `INotificationHandler<TNotification>`
- Dispatcher interfaces
- `IMediator`
- Pipeline contracts
- Validator contracts
- `Unit`
- Core exception types

Dependencies should be limited to the target framework or base class libraries.

#### `CqrsKit`

Contains runtime implementations:

- `CommandDispatcher`
- `QueryDispatcher`
- `NotificationPublisher`
- `Mediator`
- Pipeline construction and invocation
- Built-in validation behavior
- Optional diagnostic helpers
- Internal handler wrappers and caches

#### `CqrsKit.DependencyInjection`

Contains integration with `Microsoft.Extensions.DependencyInjection`:

- `AddCqrsKit` registration methods
- Options registration and validation
- Assembly scanning
- Service descriptors
- Registration diagnostics

#### `CqrsKit.Testing`

Contains optional test helpers:

- Recording behaviors
- Fake dispatchers
- Handler invocation assertions
- Pipeline-order assertions
- Test execution context

Do not force application projects to reference the testing package.

### 5.2 Simpler packaging option

A small team may initially combine `CqrsKit`, `CqrsKit.Abstractions`, and `CqrsKit.DependencyInjection` into one package. The code should still preserve namespace and folder boundaries so packages can be split later without redesigning the public API.

---

## 6. Target Framework and Language Policy

Recommended baseline:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>latestMajor</LangVersion>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <IsPackable>true</IsPackable>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

The maintainers must choose target frameworks based on the consuming estate. The public API must not accidentally depend on APIs available only in the newest target unless conditional compilation or a compatible alternative is provided.

---

## 7. Public API Specification

### 7.1 Unit

`Unit` represents successful completion without business return data.

```csharp
namespace CqrsKit;

public readonly record struct Unit
{
    public static Unit Value => default;
}
```

Requirements:

- Must be immutable.
- Must allocate no object on the managed heap in normal use.
- Must be usable as a generic response type.

### 7.2 Request abstractions

```csharp
namespace CqrsKit;

public interface ICommand<out TResponse>
{
}

public interface IQuery<out TResponse>
{
}
```

Notes:

- Commands and queries should normally be immutable records.
- The response type is part of the request's contract.
- `ICommand<Unit>` is used for commands without a business response.

### 7.3 Handler abstractions

```csharp
namespace CqrsKit;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(
        TCommand command,
        CancellationToken cancellationToken);
}

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken);
}
```

Requirements:

- A handler implementation should be stateless apart from injected dependencies.
- A handler must honor cancellation where its dependencies support it.
- A handler must not catch `OperationCanceledException` unless it rethrows it or has a documented reason to convert it.
- Exactly one closed handler must exist for each dispatched request type.

### 7.4 Dispatcher abstractions

```csharp
namespace CqrsKit;

public interface ICommandDispatcher
{
    Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default);
}

public interface IQueryDispatcher
{
    Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default);
}
```

The method names intentionally distinguish state-changing and read operations at the call site.

### 7.5 Mediator facade

```csharp
namespace CqrsKit;

public interface IMediator : ICommandDispatcher, IQueryDispatcher
{
}
```

The mediator is a convenience facade. Applications may inject `ICommandDispatcher` and `IQueryDispatcher` separately to make intent explicit.

### 7.6 Pipeline contracts

```csharp
namespace CqrsKit;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
```

Behavior contract:

1. A behavior may execute work before `next`.
2. A behavior may execute work after `next`.
3. A behavior may short-circuit and return a response without calling `next`.
4. A behavior may throw an exception.
5. A behavior must not call `next` more than once.
6. A behavior must not dispose dependencies it does not own.
7. A behavior should be safe for its registered lifetime.

### 7.7 Validator contracts

```csharp
namespace CqrsKit;

public interface IValidator<in TRequest>
{
    ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        TRequest request,
        CancellationToken cancellationToken);
}

public sealed record ValidationFailure(
    string PropertyName,
    string ErrorCode,
    string ErrorMessage,
    object? AttemptedValue = null);
```

Reasons to use a library-owned validator abstraction:

- The core does not require FluentValidation.
- Consumer projects may adapt FluentValidation or another validator.
- Pipeline semantics remain stable if the validation provider changes.

### 7.8 Validation exception

```csharp
namespace CqrsKit;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(
        Type requestType,
        IReadOnlyCollection<ValidationFailure> failures)
        : base($"Validation failed for request '{requestType.FullName}'.")
    {
        RequestType = requestType;
        Failures = failures;
    }

    public Type RequestType { get; }

    public IReadOnlyCollection<ValidationFailure> Failures { get; }
}
```

### 7.9 Resolution exceptions

```csharp
namespace CqrsKit;

public sealed class HandlerNotFoundException : InvalidOperationException
{
    public HandlerNotFoundException(Type requestType, Type handlerType)
        : base($"No handler is registered for request '{requestType.FullName}'. " +
               $"Expected service '{handlerType.FullName}'.")
    {
        RequestType = requestType;
        HandlerType = handlerType;
    }

    public Type RequestType { get; }
    public Type HandlerType { get; }
}

public sealed class DuplicateHandlerException : InvalidOperationException
{
    public DuplicateHandlerException(Type requestType, IReadOnlyCollection<Type> handlers)
        : base($"Multiple handlers are registered for request '{requestType.FullName}'.")
    {
        RequestType = requestType;
        Handlers = handlers;
    }

    public Type RequestType { get; }
    public IReadOnlyCollection<Type> Handlers { get; }
}
```

---

## 8. Runtime Implementation

### 8.1 Pipeline executor

The command and query dispatchers must share pipeline-building logic.

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace CqrsKit.Internal;

internal static class PipelineExecutor
{
    public static Task<TResponse> ExecuteAsync<TRequest, TResponse>(
        IServiceProvider serviceProvider,
        TRequest request,
        Func<Task<TResponse>> terminalHandler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(terminalHandler);

        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TRequest, TResponse>>()
            .Reverse()
            .ToArray();

        RequestHandlerDelegate<TResponse> next =
            () => terminalHandler();

        foreach (var behavior in behaviors)
        {
            var capturedNext = next;
            next = () => behavior.HandleAsync(
                request,
                capturedNext,
                cancellationToken);
        }

        return next();
    }
}
```

The resolved behavior order is reversed while building the nested delegate chain. Therefore, the first behavior registered is the outermost behavior and executes first before the handler.

### 8.2 Command dispatcher

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace CqrsKit;

internal sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public CommandDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        return SendCoreAsync((dynamic)command, cancellationToken);
    }

    private Task<TResponse> SendCoreAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        var handler = _serviceProvider
            .GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw new HandlerNotFoundException(
                typeof(TCommand),
                typeof(ICommandHandler<TCommand, TResponse>));

        return Internal.PipelineExecutor.ExecuteAsync<TCommand, TResponse>(
            _serviceProvider,
            command,
            () => handler.HandleAsync(command, cancellationToken),
            cancellationToken);
    }
}
```

### 8.3 Query dispatcher

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace CqrsKit;

internal sealed class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public QueryDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        return QueryCoreAsync((dynamic)query, cancellationToken);
    }

    private Task<TResponse> QueryCoreAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        var handler = _serviceProvider
            .GetService<IQueryHandler<TQuery, TResponse>>()
            ?? throw new HandlerNotFoundException(
                typeof(TQuery),
                typeof(IQueryHandler<TQuery, TResponse>));

        return Internal.PipelineExecutor.ExecuteAsync<TQuery, TResponse>(
            _serviceProvider,
            query,
            () => handler.HandleAsync(query, cancellationToken),
            cancellationToken);
    }
}
```

### 8.4 Mediator implementation

```csharp
namespace CqrsKit;

internal sealed class Mediator : IMediator
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public Mediator(
        ICommandDispatcher commands,
        IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default) =>
        _commands.SendAsync(command, cancellationToken);

    public Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default) =>
        _queries.QueryAsync(query, cancellationToken);
}
```

### 8.5 Runtime performance requirements

The first implementation may use `dynamic` to close the generic call. Before version 1.0, maintainers should benchmark it against a cached wrapper approach.

A higher-performance implementation may cache one request wrapper per runtime request type:

```csharp
private static readonly ConcurrentDictionary<Type, object> Wrappers = new();
```

Rules:

- Cache metadata and compiled delegates, not scoped handlers.
- Resolve scoped handlers and behaviors for every dispatch.
- Do not cache `IServiceProvider` from a child scope in a static object.
- Do not scan assemblies during dispatch.

---

## 9. Built-In Validation Pipeline

```csharp
namespace CqrsKit;

internal sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        foreach (var validator in _validators)
        {
            var result = await validator.ValidateAsync(
                request,
                cancellationToken);

            failures.AddRange(result);
        }

        if (failures.Count > 0)
        {
            throw new RequestValidationException(
                typeof(TRequest),
                failures.AsReadOnly());
        }

        return await next().ConfigureAwait(false);
    }
}
```

### 9.1 Validation semantics

- All validators registered for the request run by default.
- Validation occurs before authorization, transactions, and handler execution unless consumers configure a different order.
- Validators should be deterministic and should not modify state.
- Input-shape and application precondition validation belong here.
- Domain invariants remain in domain objects.
- Authorization is not validation.
- Missing records are normally a handler or domain-result concern, not request-shape validation.

### 9.2 FluentValidation adapter package

If required, create an optional package such as `CqrsKit.Validation.FluentValidation` that adapts FluentValidation validators to `IValidator<TRequest>`. The core package must not reference FluentValidation.

---

## 10. Consumer-Defined Pipeline Behaviors

These behaviors are specifications and examples. They should not all be in the core NuGet package.

### 10.1 Logging and performance

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        try
        {
            _logger.LogInformation(
                "Handling {RequestType}",
                typeof(TRequest).FullName);

            var response = await next().ConfigureAwait(false);

            _logger.LogInformation(
                "Handled {RequestType} in {ElapsedMilliseconds} ms",
                typeof(TRequest).FullName,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed {RequestType} after {ElapsedMilliseconds} ms",
                typeof(TRequest).FullName,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds);

            throw;
        }
    }
}
```

Do not log complete request objects by default. They may contain credentials, personal data, recipes, production data, or other confidential values.

### 10.2 Authorization

The reusable library should define no claims or roles. A consuming project can define marker interfaces:

```csharp
public interface IRequirePermission
{
    string Permission { get; }
}

public interface IRequireSiteAccess
{
    string SiteCode { get; }
}
```

Application security abstractions:

```csharp
public interface IAuthorizationService
{
    Task EnsurePermissionAsync(
        string permission,
        CancellationToken cancellationToken = default);

    Task EnsureSiteAccessAsync(
        string siteCode,
        CancellationToken cancellationToken = default);
}
```

Behavior:

```csharp
public sealed class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IAuthorizationService _authorization;

    public AuthorizationBehavior(IAuthorizationService authorization)
    {
        _authorization = authorization;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is IRequirePermission permissionRequest)
        {
            await _authorization.EnsurePermissionAsync(
                permissionRequest.Permission,
                cancellationToken);
        }

        if (request is IRequireSiteAccess siteRequest)
        {
            await _authorization.EnsureSiteAccessAsync(
                siteRequest.SiteCode,
                cancellationToken);
        }

        return await next().ConfigureAwait(false);
    }
}
```

Security rules:

- UI checks are only a usability feature. Server-side dispatch must enforce authorization.
- Never trust a site, tenant, or plant identifier supplied by a browser without checking access.
- A request-level permission does not replace resource-level authorization. If authorization depends on loaded domain data, check it after loading the resource or use a dedicated policy service.

### 10.3 Transaction behavior

The core library does not define database transaction semantics. A consumer-defined marker and unit-of-work abstraction can be used:

```csharp
public interface ITransactionalCommand
{
}

public interface IUnitOfWork
{
    Task BeginAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
```

```csharp
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand)
            return await next().ConfigureAwait(false);

        await _unitOfWork.BeginAsync(cancellationToken);

        try
        {
            var response = await next().ConfigureAwait(false);
            await _unitOfWork.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

Nested transaction policy must be documented by the consuming application.

### 10.4 Query caching

```csharp
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan TimeToLive { get; }
}
```

Caching must be optional. The consumer owns serialization, distributed caching, invalidation, tenant isolation, and sensitive-data rules.

### 10.5 Idempotency

```csharp
public interface IIdempotentCommand
{
    string IdempotencyKey { get; }
}
```

The consumer owns the durable idempotency store. In-memory idempotency is not sufficient when multiple application instances process requests.

### 10.6 Retry

Retries must not be built into the default pipeline. A command may perform non-idempotent writes or external side effects. If a retry behavior is provided, it must require an explicit marker or policy and must document interaction with transactions and idempotency.

---

## 11. Pipeline Ordering

Default recommended command order:

```text
Exception mapping or diagnostics
  -> Logging and correlation
    -> Validation
      -> Authentication/authorization
        -> Site or tenant access
          -> Idempotency
            -> Transaction
              -> Command handler
```

Default recommended query order:

```text
Exception mapping or diagnostics
  -> Logging and correlation
    -> Validation
      -> Authentication/authorization
        -> Site or tenant access
          -> Cache
            -> Query handler
```

Clarifications:

- Logging should normally wrap the full execution to capture validation and authorization failures.
- Validation may precede authorization when it only validates structure and inexpensive values. Applications concerned about information disclosure may authorize before detailed validation.
- Cache lookup must occur after access checks unless cache entries are safely isolated by user, tenant, site, and permission context.
- Transactions normally surround only command-handler execution and inner behaviors that must participate in the transaction.
- Behavior ordering must be deterministic and tested.

---

## 12. Dependency Injection API

### 12.1 Options

```csharp
namespace CqrsKit.DependencyInjection;

public sealed class CqrsKitOptions
{
    internal List<Assembly> Assemblies { get; } = [];

    public ServiceLifetime DispatcherLifetime { get; set; }
        = ServiceLifetime.Scoped;

    public ServiceLifetime HandlerLifetime { get; set; }
        = ServiceLifetime.Scoped;

    public ServiceLifetime ValidatorLifetime { get; set; }
        = ServiceLifetime.Scoped;

    public bool AddValidationBehavior { get; set; } = true;

    public bool ThrowOnDuplicateHandlers { get; set; } = true;

    public CqrsKitOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Assemblies.Add(assembly);
        return this;
    }

    public CqrsKitOptions RegisterServicesFromAssemblyContaining<T>()
    {
        Assemblies.Add(typeof(T).Assembly);
        return this;
    }
}
```

### 12.2 Registration extension

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace CqrsKit.DependencyInjection;

public static class CqrsKitServiceCollectionExtensions
{
    public static IServiceCollection AddCqrsKit(
        this IServiceCollection services,
        Action<CqrsKitOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new CqrsKitOptions();
        configure(options);

        ValidateOptions(options);

        services.Add(new ServiceDescriptor(
            typeof(ICommandDispatcher),
            typeof(CommandDispatcher),
            options.DispatcherLifetime));

        services.Add(new ServiceDescriptor(
            typeof(IQueryDispatcher),
            typeof(QueryDispatcher),
            options.DispatcherLifetime));

        services.Add(new ServiceDescriptor(
            typeof(IMediator),
            typeof(Mediator),
            options.DispatcherLifetime));

        RegisterDiscoveredTypes(services, options);

        if (options.AddValidationBehavior)
        {
            services.AddScoped(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));
        }

        return services;
    }

    private static void ValidateOptions(CqrsKitOptions options)
    {
        if (options.Assemblies.Count == 0)
            throw new InvalidOperationException(
                "At least one assembly must be registered.");
    }
}
```

The final implementation should use an options validator if options are registered through `Microsoft.Extensions.Options`. Registration methods should follow an `Add{Service}` naming convention and use the library's own namespace rather than an official Microsoft namespace.

### 12.3 Consumer registration

```csharp
services.AddCqrsKit(options =>
{
    options.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
    options.RegisterServicesFromAssemblyContaining<GetOrderQuery>();
});
```

### 12.4 Manual registration

The library must permit manual registration and must not require assembly scanning:

```csharp
services.AddScoped<
    ICommandHandler<CreateOrderCommand, Result<Guid>>,
    CreateOrderCommandHandler>();

services.AddScoped<
    IQueryHandler<GetOrderQuery, Result<OrderDto>>,
    GetOrderQueryHandler>();

services.AddScoped<
    IValidator<CreateOrderCommand>,
    CreateOrderCommandValidator>();
```

---

## 13. Assembly Scanning Rules

At registration time, scan only assemblies explicitly supplied by the consumer.

A handler candidate must:

- Be a concrete, non-abstract class.
- Not be an open generic type unless explicitly supported.
- Implement a closed `ICommandHandler<,>` or `IQueryHandler<,>` interface.
- Be activatable by DI.

A validator candidate must:

- Be a concrete class.
- Implement one or more closed `IValidator<TRequest>` interfaces.

Duplicate detection:

- More than one command handler for the same command type is an error.
- More than one query handler for the same query type is an error.
- Multiple validators for the same request are allowed.
- Multiple pipeline behaviors for the same request/response are allowed.

Trimming and native AOT:

- Reflection scanning can conflict with trimming and native AOT.
- Manual registration must remain supported.
- A future source-generator package may emit registrations at compile time.
- If reflection scanning is retained, document trimming annotations and publish warnings.

---

## 14. Service Lifetimes and Scopes

Recommended defaults:

- Dispatchers: scoped.
- Mediator: scoped.
- Handlers: scoped.
- Validators: scoped or transient.
- Pipeline behaviors: scoped.

Rationale:

- Application handlers frequently depend on scoped database contexts, current-user services, tenant context, or unit-of-work services.
- Singleton dispatchers must not capture scoped services.
- Runtime caches may be singleton only when they contain immutable metadata or compiled delegates and never scoped service instances.

Applications must enable scope validation in development and automated tests where possible.

---

## 15. Cancellation, Exceptions, and Result Types

### 15.1 Cancellation

- Dispatchers must check the token before handler resolution or execution.
- The same token must flow through every behavior, validator, and handler.
- Cancellation should surface as `OperationCanceledException` or `TaskCanceledException` with the relevant token.
- The library must not convert cancellation into validation or handler errors.

### 15.2 Exception policy

The core library should throw only for technical or configuration failures it owns:

- `HandlerNotFoundException`
- `DuplicateHandlerException`
- `RequestValidationException`
- Invalid library options

Business failures should be represented by the consuming application's result type or domain exceptions.

### 15.3 Optional result model

The library should not force every consumer to use one business result model. If a small result abstraction is included, place it in an optional package or document it as a sample:

```csharp
public sealed record Error(string Code, string Message);

public sealed class Result<T>
{
    private Result(bool success, T? value, Error? error)
    {
        IsSuccess = success;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) =>
        new(true, value, null);

    public static Result<T> Failure(Error error) =>
        new(false, default, error);
}
```

---

## 16. Diagnostics and Observability

The library should provide stable diagnostic names without forcing a logging implementation.

Recommended activities:

```text
CqrsKit.Command
CqrsKit.Query
```

Recommended tags:

```text
cqrs.request.type
cqrs.request.kind
cqrs.handler.type
cqrs.outcome
cqrs.validation.failure_count
```

Requirements:

- Do not record request property values by default.
- Do not record response bodies by default.
- Do not expose secrets or personal data.
- Allow consuming applications to add correlation, tenant, site, or user tags through their own behavior.
- Activity creation should be optional and low overhead when no listener is active.

---

## 17. Example Feature in a Consuming Project

### 17.1 Command

```csharp
public sealed record CloseProductionOrderCommand(
    int OrderId,
    string SiteCode)
    : ICommand<Result>,
      ITransactionalCommand,
      IRequirePermission,
      IRequireSiteAccess
{
    public string Permission => "Production.CloseOrder";
}
```

### 17.2 Validator

```csharp
public sealed class CloseProductionOrderValidator
    : IValidator<CloseProductionOrderCommand>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        CloseProductionOrderCommand request,
        CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        if (request.OrderId <= 0)
        {
            failures.Add(new ValidationFailure(
                nameof(request.OrderId),
                "OrderId.Invalid",
                "OrderId must be greater than zero.",
                request.OrderId));
        }

        if (string.IsNullOrWhiteSpace(request.SiteCode))
        {
            failures.Add(new ValidationFailure(
                nameof(request.SiteCode),
                "SiteCode.Required",
                "SiteCode is required."));
        }

        return ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>(
            failures.AsReadOnly());
    }
}
```

### 17.3 Handler

```csharp
public sealed class CloseProductionOrderHandler
    : ICommandHandler<CloseProductionOrderCommand, Result>
{
    private readonly IProductionOrderRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CloseProductionOrderHandler(
        IProductionOrderRepository repository,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Result> HandleAsync(
        CloseProductionOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(
            command.OrderId,
            cancellationToken);

        if (order is null)
            return Result.Failure("Production order was not found.");

        if (!string.Equals(
            order.SiteCode,
            command.SiteCode,
            StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure(
                "The production order does not belong to the requested site.");
        }

        order.Close(_timeProvider.GetUtcNow());

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
```

### 17.4 Query

```csharp
public sealed record GetProductionOrderQuery(
    int OrderId,
    string SiteCode)
    : IQuery<Result<ProductionOrderDto>>,
      IRequirePermission,
      IRequireSiteAccess
{
    public string Permission => "Production.ViewOrder";
}
```

### 17.5 Query handler

```csharp
public sealed class GetProductionOrderHandler
    : IQueryHandler<GetProductionOrderQuery, Result<ProductionOrderDto>>
{
    private readonly IProductionOrderReadService _readService;

    public GetProductionOrderHandler(
        IProductionOrderReadService readService)
    {
        _readService = readService;
    }

    public async Task<Result<ProductionOrderDto>> HandleAsync(
        GetProductionOrderQuery query,
        CancellationToken cancellationToken)
    {
        var order = await _readService.FindAsync(
            query.OrderId,
            query.SiteCode,
            cancellationToken);

        return order is null
            ? Result<ProductionOrderDto>.Failure(
                "Production order was not found.")
            : Result<ProductionOrderDto>.Success(order);
    }
}
```

### 17.6 Calling code

```csharp
var commandResult = await commandDispatcher.SendAsync(
    new CloseProductionOrderCommand(orderId, siteCode),
    cancellationToken);

var queryResult = await queryDispatcher.QueryAsync(
    new GetProductionOrderQuery(orderId, siteCode),
    cancellationToken);
```

---

## 18. Blazor Web App Considerations

For a Blazor Web App using Interactive Auto:

- Components running interactively on the server may inject server-side dispatchers.
- Components executing through WebAssembly cannot directly use handlers that depend on server-side repositories, database contexts, secrets, or infrastructure services.
- Client-side components should call secured server endpoints.
- Authorization must be enforced again on the server.
- Keep CQRS request contracts shared only when they are safe to distribute to the browser.
- Do not place server-only handlers in the client project.
- Do not treat a disabled or hidden button as authorization.

A practical split is:

```text
MyApp.Contracts
  Safe request/response DTOs used across HTTP boundaries

MyApp.Application
  Server-side commands, queries, handlers, policies

MyApp.Web.Client
  UI and typed API clients

MyApp.Web
  HTTP endpoints and server-side dispatch
```

---

## 19. Testing Specification

### 19.1 Unit tests

Required test groups:

#### Dispatch

- Dispatches a command to the correct handler.
- Dispatches a query to the correct handler.
- Returns the handler response unchanged.
- Supports `Unit` responses.
- Throws when a handler is missing.
- Rejects null requests.

#### Pipelines

- Executes behaviors in registration order before the handler.
- Executes behaviors in reverse order after the handler.
- Allows short-circuiting.
- Propagates handler exceptions.
- Propagates behavior exceptions.
- Does not invoke the handler after short-circuiting.
- Passes the same cancellation token to all elements.

#### Validation

- Executes all validators for a request.
- Aggregates all failures.
- Skips handler execution when validation fails.
- Executes handler when no validators are registered.
- Executes handler when validators return no failures.

#### Registration

- Registers dispatchers and mediator.
- Discovers handlers only from configured assemblies.
- Registers multiple validators.
- Detects duplicate command handlers.
- Detects duplicate query handlers.
- Ignores abstract classes and interfaces.
- Honors configured service lifetimes.

#### Scope safety

- Resolves scoped handlers inside a valid scope.
- Does not retain scoped services in static caches.
- Passes .NET scope validation.

### 19.2 Pipeline-order test example

```csharp
[Fact]
public async Task Behaviors_execute_in_expected_order()
{
    var events = new List<string>();

    // Register BehaviorA, BehaviorB, then handler.
    // Each behavior records before and after next().

    await mediator.SendAsync(new TestCommand());

    events.Should().Equal(
        "A-before",
        "B-before",
        "handler",
        "B-after",
        "A-after");
}
```

### 19.3 Integration tests

Use a real `ServiceCollection` and scope. Validate:

- Assembly scanning.
- Open-generic behavior registration.
- Multiple validators.
- Scoped dependencies.
- Cancellation.
- Logging and activity creation.
- Packaging and public API consumption from a sample project.

### 19.4 Architecture tests

Verify:

- `CqrsKit.Abstractions` does not reference runtime, ASP.NET Core, EF Core, or consumers.
- Public abstractions remain in approved namespaces.
- Runtime internals are not accidentally public.
- Optional packages depend inward on abstractions.

---

## 20. NuGet Packaging Specification

Each package should include:

- Package ID and version.
- Description and release notes.
- Repository URL and commit metadata.
- License expression or license file.
- README.
- XML documentation.
- Symbols package.
- Source Link.
- Package icon if the organization requires one.

Example project properties:

```xml
<PropertyGroup>
  <PackageId>CqrsKit</PackageId>
  <Title>CqrsKit</Title>
  <Description>Lightweight CQRS and mediator abstractions, dispatchers, pipelines, and validation for .NET.</Description>
  <Authors>Your Organization</Authors>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryType>git</RepositoryType>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
  <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

For an internal library, replace public repository and feed settings with the organization's approved equivalents.

---

## 21. Versioning and Compatibility

Use semantic versioning:

- **Major:** breaking public API or behavioral changes.
- **Minor:** backward-compatible functionality.
- **Patch:** backward-compatible fixes.

Breaking changes include:

- Renaming public interfaces or methods.
- Changing generic constraints.
- Changing default behavior order.
- Changing exception types for established failure cases.
- Changing registration lifetime defaults.
- Removing public members.

Maintain a `CHANGELOG.md` and migration notes for every major release.

For public API compatibility, consider:

- API baseline files.
- Package validation during build.
- Binary compatibility checks.
- Consumer contract tests against supported target frameworks.

---

## 22. CI/CD Quality Gates

Every pull request should run:

```bash
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet pack --configuration Release --no-build
```

Recommended gates:

- Build with warnings as errors.
- Unit, integration, and architecture tests pass.
- Package can be installed by a clean sample project.
- Public API compatibility check passes.
- Code formatting check passes.
- Dependency vulnerability scan passes.
- Package metadata validation passes.
- Benchmarks run for release candidates or performance-sensitive changes.

---

## 23. Security Requirements

- The library must not log request or response data by default.
- The library must not assume authorization based on UI state.
- Pipeline behaviors must not expose secrets through exception messages.
- Assembly scanning must use explicitly supplied assemblies.
- Consumers must validate site, tenant, plant, or customer scope using trusted identity or data sources.
- Cache keys and entries must isolate tenants and security scopes.
- The library must not retry a non-idempotent command automatically.
- Idempotency stores must be durable when commands can be processed by multiple instances.
- Request types shared with browser clients must contain no secrets.
- Security updates should follow the repository's `SECURITY.md` process.

---

## 24. Performance Requirements

The implementation must:

- Avoid assembly scanning after startup.
- Avoid repeated reflection for the same request type where practical.
- Avoid caching scoped service instances.
- Avoid allocation-heavy logging when logging is disabled.
- Avoid creating diagnostic activities when no listener is active.
- Avoid parallel validator execution by default because validators may depend on non-thread-safe scoped services.
- Support benchmarks for dispatch overhead with zero, one, and multiple behaviors.

Suggested benchmark scenarios:

- Direct handler call.
- Dispatch without behaviors.
- Dispatch with validation behavior and no validators.
- Dispatch with three behaviors.
- First dispatch versus cached subsequent dispatch.
- Concurrent dispatches across independent scopes.

Performance should be evaluated against application needs. Correct scope handling, cancellation, and predictable behavior are more important than micro-optimizing a path dominated by database or network access.

---

## 25. Documentation Requirements

The repository must provide:

1. Installation and registration.
2. Command example.
3. Query example.
4. Validator example.
5. Behavior example.
6. Behavior-order explanation.
7. Manual registration example.
8. Assembly scanning example.
9. Exception and cancellation behavior.
10. Blazor WebAssembly and Interactive Auto limitations.
11. Testing guidance.
12. Migration guide from direct service calls or MediatR.
13. Versioning policy.
14. Security considerations.

All public APIs require XML documentation.

---

## 26. Implementation Phases

### Phase 1: Core MVP

- Abstractions.
- `Unit`.
- Command dispatcher.
- Query dispatcher.
- Mediator facade.
- Pipeline execution.
- Manual DI registration.
- Validation contracts and behavior.
- Core exceptions.
- Unit tests.

### Phase 2: Registration and diagnostics

- Assembly scanning.
- Duplicate detection.
- Options and startup validation.
- ActivitySource diagnostics.
- Integration tests.
- NuGet packaging.

### Phase 3: Ecosystem packages

- FluentValidation adapter.
- ASP.NET Core exception-to-ProblemDetails integration.
- Testing utilities.
- Source-generated registration for trimming and native AOT.

---

## 27. Acceptance Criteria

The library is ready for version 1.0 when:

- [ ] A command is dispatched to exactly one handler.
- [ ] A query is dispatched to exactly one handler.
- [ ] `Unit` commands work.
- [ ] Behavior ordering is deterministic and documented.
- [ ] Validators aggregate failures and prevent handler execution.
- [ ] Cancellation reaches validators, behaviors, and handlers.
- [ ] Missing handlers generate actionable exceptions.
- [ ] Duplicate handlers fail registration.
- [ ] Manual and scanned registration both work.
- [ ] Scope validation detects no captive dependency in the library.
- [ ] No ASP.NET Core, EF Core, Blazor, or identity dependency exists in the core package.
- [ ] Public APIs have XML documentation.
- [ ] Packages include symbols and Source Link.
- [ ] Sample console, Web API, and Blazor applications compile and run.
- [ ] Package installation is tested from a clean solution.
- [ ] CI quality gates pass.
- [ ] Security and migration documentation exists.
- [ ] Benchmarks establish an initial performance baseline.

---

## 28. Recommended Final Decisions

Use these defaults unless the implementing team has a specific reason to change them:

- Separate `ICommandDispatcher`, `IQueryDispatcher`, and `INotificationPublisher`, plus optional `IMediator` facade.
- In-process notifications: sequential handlers for the runtime type, stop on the first exception, no cross-process delivery.
- Generic requests only: `ICommand<TResponse>` and `IQuery<TResponse>`.
- `Unit` for commands without return data.
- `Task<T>` public asynchronous APIs.
- Library-owned validator abstraction with optional provider adapters.
- One handler per command or query.
- Multiple ordered behaviors and validators.
- Scoped dispatchers, handlers, validators, and behaviors.
- Explicit assembly list for scanning.
- Startup duplicate detection.
- No automatic retries, transactions, caching, or authorization in the core.
- Optional integrations implemented outside the core library.
- Manual registration preserved for trimming, native AOT, and maximum transparency.

---

## 29. Reference and Rationale Notes

The design uses built-in .NET dependency injection concepts: abstractions are registered in `IServiceCollection`, resolved from `IServiceProvider`, and injected into constructors. Microsoft documentation describes DI as a built-in .NET pattern for inversion of control and warns against hard-coded dependencies because they are difficult to replace and test.

The registration API follows .NET library-author guidance by using an `Add{Service}` extension method and strongly typed options. The package uses its own namespace rather than the `Microsoft.Extensions.DependencyInjection` namespace because it is not an official Microsoft package.

The implementation should use the built-in service container unless a consumer requires unsupported container features. Scope validation is important because a singleton must not capture scoped handlers, behaviors, database contexts, or user-context services.

### External references

- [.NET dependency injection overview](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/overview)
- [Dependency injection guidelines](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection/guidelines)
- [Options pattern guidance for .NET library authors](https://learn.microsoft.com/dotnet/core/extensions/options-library-authors)
- [NuGet package creation overview](https://learn.microsoft.com/nuget/create-packages/overview-and-workflow)

---

## 30. Final Consumer Quick Start

```bash
dotnet add package CqrsKit
dotnet add package CqrsKit.DependencyInjection
```

```csharp
builder.Services.AddCqrsKit(options =>
{
    options.RegisterServicesFromAssemblyContaining<CreateOrderCommand>();
});
```

```csharp
public sealed record CreateOrderCommand(string OrderNumber)
    : ICommand<Guid>;

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

```csharp
var orderId = await mediator.SendAsync(
    new CreateOrderCommand("PO-10001"),
    cancellationToken);
```

This quick start demonstrates the minimum path. Production applications should also configure validation, authorization, logging, and appropriate command transaction policies.
