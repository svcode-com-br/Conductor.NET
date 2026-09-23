using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Internal;

internal abstract class NotificationHandlerWrapper
{
    public abstract Task PublishAsync(
        INotification notification,
        IServiceProvider services,
        Activity? activity,
        CancellationToken cancellationToken);
}

internal sealed class NotificationHandlerWrapperImpl<TNotification> : NotificationHandlerWrapper
    where TNotification : INotification
{
    public override async Task PublishAsync(
        INotification notification,
        IServiceProvider services,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var typed = (TNotification)notification;
        var handlers = services.GetServices<INotificationHandler<TNotification>>().ToArray();
        ConductorDiagnostics.SetHandlerCount(activity, handlers.Length);

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await handler.HandleAsync(typed, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                ConductorDiagnostics.SetHandlerType(activity, handler.GetType());
                throw;
            }
        }
    }
}
