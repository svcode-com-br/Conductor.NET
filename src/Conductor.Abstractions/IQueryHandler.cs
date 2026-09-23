namespace Conductor;

/// <summary>
/// Handles a single closed query type.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="query">The query instance.</param>
    /// <param name="cancellationToken">The token used to cancel handler execution.</param>
    /// <returns>The handler response.</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
