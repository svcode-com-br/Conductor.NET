using Conductor;
using Conductor.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Tests;

internal sealed record PingCommand(string Message) : ICommand<string>;

internal sealed record UnitCommand : ICommand<Unit>;

internal sealed record GetMessageQuery(string Id) : IQuery<string>;

internal sealed class PingCommandHandler : ICommandHandler<PingCommand, string>
{
    public int Calls { get; private set; }

    public Task<string> HandleAsync(PingCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        return Task.FromResult(command.Message);
    }
}

internal sealed class UnitCommandHandler : ICommandHandler<UnitCommand, Unit>
{
    public Task<Unit> HandleAsync(UnitCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Unit.Value);
}

internal sealed class GetMessageQueryHandler : IQueryHandler<GetMessageQuery, string>
{
    public Task<string> HandleAsync(GetMessageQuery query, CancellationToken cancellationToken) =>
        Task.FromResult($"msg:{query.Id}");
}

internal sealed class ThrowingCommand : ICommand<Unit>;

internal sealed class ThrowingCommandHandler : ICommandHandler<ThrowingCommand, Unit>
{
    public Task<Unit> HandleAsync(ThrowingCommand command, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("handler-failed");
}

internal static class TestServiceProvider
{
    public static ServiceProvider Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddConductor(options =>
        {
            options.AddValidationBehavior = false;
        });
        configure?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }
}
