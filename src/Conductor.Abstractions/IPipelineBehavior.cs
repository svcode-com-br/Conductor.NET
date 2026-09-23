namespace Conductor;

/// <summary>
/// A cross-cutting pipeline component that wraps handler execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// A behavior may run logic before and after the next delegate, short-circuit without
/// calling next, or throw. It must not invoke next more than once.
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Handles the current pipeline step.
    /// </summary>
    /// <param name="request">The request being dispatched.</param>
    /// <param name="next">The next pipeline delegate. Call at most once.</param>
    /// <param name="cancellationToken">The token used to cancel the pipeline.</param>
    /// <returns>The pipeline response.</returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}
