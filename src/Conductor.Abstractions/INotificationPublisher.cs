namespace Conductor;

/// <summary>
/// Publishes an in-process notification to every handler registered for its runtime type.
/// </summary>
public interface INotificationPublisher
{
    /// <summary>
    /// Publishes a notification. Handlers run sequentially in registration order.
    /// Zero handlers is a successful no-op. The first exception stops the remaining handlers.
    /// </summary>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The token used to cancel publishing.</param>
    /// <returns>A task that completes when every invoked handler has finished.</returns>
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}
