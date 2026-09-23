using Conductor;

namespace Conductor.Testing;

/// <summary>
/// In-memory command dispatcher for unit tests that do not host the real pipeline.
/// </summary>
public sealed class FakeCommandDispatcher : ICommandDispatcher
{
    private readonly List<object> _sent = [];
    private readonly Dictionary<Type, Func<object, CancellationToken, Task<object?>>> _handlers = [];

    /// <summary>
    /// Gets the commands that were sent, in call order.
    /// </summary>
    public IReadOnlyList<object> SentCommands => _sent;

    /// <summary>
    /// Configures the response produced for <typeparamref name="TCommand"/>.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The fake handler.</param>
    /// <returns>The same dispatcher.</returns>
    public FakeCommandDispatcher On<TCommand, TResponse>(
        Func<TCommand, CancellationToken, Task<TResponse>> handler)
        where TCommand : ICommand<TResponse>
    {
        ArgumentNullException.ThrowIfNull(handler);
        _handlers[typeof(TCommand)] = async (command, cancellationToken) =>
            await handler((TCommand)command, cancellationToken).ConfigureAwait(false);
        return this;
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        _sent.Add(command);

        if (!_handlers.TryGetValue(command.GetType(), out var handler))
        {
            return default!;
        }

        var result = await handler(command, cancellationToken).ConfigureAwait(false);
        return (TResponse)result!;
    }
}
