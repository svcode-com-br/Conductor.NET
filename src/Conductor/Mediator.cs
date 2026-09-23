namespace Conductor;

internal sealed class Mediator : IMediator
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;
    private readonly INotificationPublisher _notifications;

    public Mediator(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        INotificationPublisher notifications)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentNullException.ThrowIfNull(notifications);

        _commands = commands;
        _queries = queries;
        _notifications = notifications;
    }

    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default) =>
        _commands.SendAsync(command, cancellationToken);

    public Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default) =>
        _queries.QueryAsync(query, cancellationToken);

    public Task PublishAsync(
        INotification notification,
        CancellationToken cancellationToken = default) =>
        _notifications.PublishAsync(notification, cancellationToken);
}
