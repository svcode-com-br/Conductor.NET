using Conductor;

namespace Conductor.TestSupport.Duplicates;

public sealed record DuplicateCommand : ICommand<Unit>;

public sealed class DuplicateCommandHandlerA : ICommandHandler<DuplicateCommand, Unit>
{
    public Task<Unit> HandleAsync(DuplicateCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Unit.Value);
}

public sealed class DuplicateCommandHandlerB : ICommandHandler<DuplicateCommand, Unit>
{
    public Task<Unit> HandleAsync(DuplicateCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Unit.Value);
}

public sealed record DuplicateQuery : IQuery<int>;

public sealed class DuplicateQueryHandlerA : IQueryHandler<DuplicateQuery, int>
{
    public Task<int> HandleAsync(DuplicateQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(1);
}

public sealed class DuplicateQueryHandlerB : IQueryHandler<DuplicateQuery, int>
{
    public Task<int> HandleAsync(DuplicateQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(2);
}
