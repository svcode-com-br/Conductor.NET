using Conductor;

namespace Conductor.TestSupport.Scan;

public sealed record ScannedCommand(string Name) : ICommand<string>;

public sealed class ScannedCommandHandler : ICommandHandler<ScannedCommand, string>
{
    public Task<string> HandleAsync(ScannedCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(command.Name);
}

public sealed record ScannedQuery : IQuery<int>;

public sealed class ScannedQueryHandler : IQueryHandler<ScannedQuery, int>
{
    public Task<int> HandleAsync(ScannedQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(7);
}

public sealed class ScannedCommandValidator : IValidator<ScannedCommand>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        ScannedCommand request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>([]);
}

public sealed record ScannedNotification(string Name) : INotification;

public sealed class FirstScannedNotificationHandler : INotificationHandler<ScannedNotification>
{
    public Task HandleAsync(ScannedNotification notification, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class SecondScannedNotificationHandler : INotificationHandler<ScannedNotification>
{
    public Task HandleAsync(ScannedNotification notification, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class SecondScannedCommandValidator : IValidator<ScannedCommand>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        ScannedCommand request,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>([]);
}

public abstract class AbstractCommandHandler : ICommandHandler<ScannedCommand, string>
{
    public abstract Task<string> HandleAsync(ScannedCommand command, CancellationToken cancellationToken);
}

public sealed class OpenGenericHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    public Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
