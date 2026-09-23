using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Tests;

public sealed class DispatchTests
{
    [Fact]
    public async Task SendAsync_dispatches_command_to_handler_and_returns_response()
    {
        var handler = new PingCommandHandler();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>>(handler);
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.SendAsync(new PingCommand("hello"));

        Assert.Equal("hello", result);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task QueryAsync_dispatches_query_to_handler()
    {
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<IQueryHandler<GetMessageQuery, string>, GetMessageQueryHandler>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        var result = await dispatcher.QueryAsync(new GetMessageQuery("42"));

        Assert.Equal("msg:42", result);
    }

    [Fact]
    public async Task Mediator_delegates_to_command_and_query_dispatchers()
    {
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>, PingCommandHandler>();
            services.AddSingleton<IQueryHandler<GetMessageQuery, string>, GetMessageQueryHandler>();
        });

        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        Assert.Equal("x", await mediator.SendAsync(new PingCommand("x")));
        Assert.Equal("msg:y", await mediator.QueryAsync(new GetMessageQuery("y")));
    }

    [Fact]
    public async Task SendAsync_supports_unit_response()
    {
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<ICommandHandler<UnitCommand, Unit>, UnitCommandHandler>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.SendAsync(new UnitCommand());

        Assert.Equal(Unit.Value, result);
    }

    [Fact]
    public async Task SendAsync_throws_when_handler_is_missing()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => dispatcher.SendAsync(new PingCommand("missing")));

        Assert.Equal(typeof(PingCommand), exception.RequestType);
        Assert.Equal(typeof(ICommandHandler<PingCommand, string>), exception.HandlerType);
    }

    [Fact]
    public async Task QueryAsync_throws_when_handler_is_missing()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => dispatcher.QueryAsync(new GetMessageQuery("missing")));
    }

    [Fact]
    public async Task SendAsync_rejects_null_command()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.SendAsync<string>(null!));
    }

    [Fact]
    public async Task QueryAsync_rejects_null_query()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.QueryAsync<string>(null!));
    }
}
