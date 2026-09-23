using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Internal;

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

        RequestHandlerDelegate<TResponse> next = () => terminalHandler();

        foreach (var behavior in behaviors)
        {
            var capturedNext = next;
            var capturedBehavior = behavior;
            next = () => capturedBehavior.HandleAsync(
                request,
                capturedNext,
                cancellationToken);
        }

        return next();
    }
}
