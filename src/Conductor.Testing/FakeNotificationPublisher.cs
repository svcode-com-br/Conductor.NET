using Conductor;

namespace Conductor.Testing;

/// <summary>
/// In-memory notification publisher for unit tests that do not host the real pipeline.
/// </summary>
public sealed class FakeNotificationPublisher : INotificationPublisher
{
    private readonly List<object> _published = [];
    private readonly Dictionary<Type, List<Func<object, CancellationToken, Task>>> _handlers = [];

    /// <summary>
    /// Gets the notifications that were published, in call order.
    /// </summary>
    public IReadOnlyList<object> PublishedNotifications => _published;

    /// <summary>
    /// Adds a handler invoked when <typeparamref name="TNotification"/> is published.
    /// Handlers run in the order they are added. The first exception stops the rest.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="handler">The fake handler.</param>
    /// <returns>The same publisher.</returns>
    public FakeNotificationPublisher On<TNotification>(
        Func<TNotification, CancellationToken, Task> handler)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryGetValue(typeof(TNotification), out var handlers))
        {
            handlers = [];
            _handlers[typeof(TNotification)] = handlers;
        }

        handlers.Add((notification, cancellationToken) =>
            handler((TNotification)notification, cancellationToken));
        return this;
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        _published.Add(notification);

        if (!_handlers.TryGetValue(notification.GetType(), out var handlers))
        {
            return;
        }

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler(notification, cancellationToken).ConfigureAwait(false);
        }
    }
}
