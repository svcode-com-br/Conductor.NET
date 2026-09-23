namespace Conductor;

/// <summary>
/// Handles one closed notification type. Multiple handlers may be registered for the same type.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification instance.</param>
    /// <param name="cancellationToken">The token used to cancel handler execution.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    Task HandleAsync(TNotification notification, CancellationToken cancellationToken);
}
