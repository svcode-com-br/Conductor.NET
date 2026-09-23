using Conductor;

namespace Conductor.Testing;

/// <summary>
/// In-memory query dispatcher for unit tests that do not host the real pipeline.
/// </summary>
public sealed class FakeQueryDispatcher : IQueryDispatcher
{
    private readonly List<object> _sent = [];
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = [];

    /// <summary>
    /// Gets the queries that were executed, in call order.
    /// </summary>
    public IReadOnlyList<object> SentQueries => _sent;

    /// <summary>
    /// Configures the response produced for <typeparamref name="TQuery"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The fake handler.</param>
    /// <returns>The same dispatcher.</returns>
    public FakeQueryDispatcher On<TQuery, TResponse>(
        Func<TQuery, CancellationToken, Task<TResponse>> handler)
        where TQuery : IQuery<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[typeof(TQuery)] = async (query, cancellationToken) =>
            await handler((TQuery)query, cancellationToken).ConfigureAwait(false);
        return this;
    }

    /// <inheritdoc />
    public async Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();

        _sent.Add(query);

        if (!_handlers.TryGetValue(query.GetType(), out var handler))
        {
            return default!;
        }

        var result = await handler(query, cancellationToken).ConfigureAwait(false);
        return (TResponse)result!;
    }
}
