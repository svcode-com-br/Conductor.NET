using System.Collections.Concurrent;
using Conductor.Internal;

namespace Conductor;

internal sealed class NotificationPublisher : INotificationPublisher
{
    private static readonly ConcurrentDictionary<Type, NotificationHandlerWrapper> Wrappers = new();

    private readonly IServiceProvider _serviceProvider;

    public NotificationPublisher(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        cancellationToken.ThrowIfCancellationRequested();

        var notificationType = notification.GetType();
        var wrapper = GetOrAddWrapper(notificationType);
        using var activity = ConductorDiagnostics.StartNotification(notificationType);

        try
        {
            await wrapper
                .PublishAsync(notification, _serviceProvider, activity, cancellationToken)
                .ConfigureAwait(false);

            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeSuccess);
        }
        catch (OperationCanceledException)
        {
            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeCanceled);
            throw;
        }
        catch
        {
            ConductorDiagnostics.SetOutcome(activity, ConductorTelemetry.OutcomeError);
            throw;
        }
    }

    private static NotificationHandlerWrapper GetOrAddWrapper(Type notificationType)
    {
        return Wrappers.GetOrAdd(notificationType, type =>
        {
            var wrapperType = typeof(NotificationHandlerWrapperImpl<>).MakeGenericType(type);
            return (NotificationHandlerWrapper)(Activator.CreateInstance(wrapperType)
                ?? throw new InvalidOperationException($"Unable to create handler wrapper for '{type.FullName}'."));
        });
    }
}
