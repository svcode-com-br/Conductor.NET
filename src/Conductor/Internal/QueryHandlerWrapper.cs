using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Internal;

internal abstract class QueryHandlerWrapper<TResponse>
{
    public abstract Task<TResponse> HandleAsync(
        IQuery<TResponse> query,
        IServiceProvider services,
        CancellationToken cancellationToken);
}

internal sealed class QueryHandlerWrapperImpl<TQuery, TResponse> : QueryHandlerWrapper<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override Task<TResponse> HandleAsync(
        IQuery<TResponse> query,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var typed = (TQuery)query;
        var handler = services.GetService<IQueryHandler<TQuery, TResponse>>()
            ?? throw new HandlerNotFoundException(
                typeof(TQuery),
                typeof(IQueryHandler<TQuery, TResponse>));

        ConductorDiagnostics.SetHandlerType(System.Diagnostics.Activity.Current, handler.GetType());

        return PipelineExecutor.ExecuteAsync(
            services,
            typed,
            () => handler.HandleAsync(typed, cancellationToken),
            cancellationToken);
    }
}
