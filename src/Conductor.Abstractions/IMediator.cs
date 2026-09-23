namespace Conductor;

/// <summary>
/// Convenience facade that exposes command dispatch, query dispatch, and notification publishing.
/// </summary>
/// <remarks>
/// Applications that want intent to be explicit at the call site may inject
/// <see cref="ICommandDispatcher"/>, <see cref="IQueryDispatcher"/>, and
/// <see cref="INotificationPublisher"/> separately.
/// </remarks>
public interface IMediator : ICommandDispatcher, IQueryDispatcher, INotificationPublisher
{
}
