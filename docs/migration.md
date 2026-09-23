# Migration

## From direct service calls

Replace injected application services at the edge (controllers, endpoints, workers) with `ICommandDispatcher` / `IQueryDispatcher`. Move use-case methods onto `ICommandHandler<,>` or `IQueryHandler<,>`. Cross-cutting concerns become pipeline behaviors instead of duplicated try/catch or logging in every method.

## From MediatR

| MediatR | Conductor.NET |
| --- | --- |
| `IRequest<T>` / `IRequestHandler<,>` | `ICommand<T>` / `IQuery<T>` and matching handlers |
| `IMediator.Send` | `SendAsync` for commands, `QueryAsync` for queries |
| `IPipelineBehavior<,>` | `IPipelineBehavior<,>` (`HandleAsync`, `RequestHandlerDelegate<T>`) |
| `INotification` / `Publish` | `INotification` / `PublishAsync` |
| FluentValidation package | `IValidator<T>` in core; adapt FluentValidation in the app if needed |

Streaming requests and request pre/post processors are not provided. Prefer explicit command, query, and notification types over a single `IRequest` marker.

`PublishAsync` is in-process only. It runs handlers for the notification's runtime type sequentially, in registration order, and stops on the first exception. It does not fan out to base-type handlers, run handlers in parallel, or continue after a failure.

Manual registration remains the trimming-safe alternative to reflection scanning. Source-generated registration is not included in this release.
