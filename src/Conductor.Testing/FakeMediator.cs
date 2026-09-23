using Conductor;

namespace Conductor.Testing;

/// <summary>
/// Facade over fake command and query dispatchers and a fake notification publisher.
/// </summary>
public sealed class FakeMediator : IMediator
{
    /// <summary>
    /// Initializes a mediator that uses dedicated fake dispatchers and a fake publisher.
    /// </summary>
    public FakeMediator()
        : this(new FakeCommandDispatcher(), new FakeQueryDispatcher(), new FakeNotificationPublisher())
    {
    }

    /// <summary>
    /// Initializes a mediator around existing fake command and query dispatchers.
    /// </summary>
    /// <param name="commands">The fake command dispatcher.</param>
    /// <param name="queries">The fake query dispatcher.</param>
    public FakeMediator(FakeCommandDispatcher commands, FakeQueryDispatcher queries)
        : this(commands, queries, new FakeNotificationPublisher())
    {
    }

    /// <summary>
    /// Initializes a mediator around existing fake dispatchers and a fake publisher.
    /// </summary>
    /// <param name="commands">The fake command dispatcher.</param>
    /// <param name="queries">The fake query dispatcher.</param>
    /// <param name="notifications">The fake notification publisher.</param>
    public FakeMediator(
        FakeCommandDispatcher commands,
        FakeQueryDispatcher queries,
        FakeNotificationPublisher notifications)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentNullException.ThrowIfNull(notifications);

        Commands = commands;
        Queries = queries;
        Notifications = notifications;
    }

    /// <summary>
    /// Gets the underlying fake command dispatcher.
    /// </summary>
    public FakeCommandDispatcher Commands { get; }

    /// <summary>
    /// Gets the underlying fake query dispatcher.
    /// </summary>
    public FakeQueryDispatcher Queries { get; }

    /// <summary>
    /// Gets the underlying fake notification publisher.
    /// </summary>
    public FakeNotificationPublisher Notifications { get; }

    /// <inheritdoc />
    public Task<TResponse> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken cancellationToken = default) =>
        Commands.SendAsync(command, cancellationToken);

    /// <inheritdoc />
    public Task<TResponse> QueryAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken cancellationToken = default) =>
        Queries.QueryAsync(query, cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(
        INotification notification,
        CancellationToken cancellationToken = default) =>
        Notifications.PublishAsync(notification, cancellationToken);
}
