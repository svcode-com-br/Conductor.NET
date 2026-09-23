namespace Conductor;

/// <summary>
/// Dispatches queries to exactly one handler through the registered pipeline.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Executes a query against its handler.
    /// </summary>
    /// <typeparam name="TResponse">The query response type.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">The token used to cancel dispatch.</param>
    /// <returns>The handler response.</returns>
    Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default);
}
