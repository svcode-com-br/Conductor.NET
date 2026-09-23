using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task Behaviors_execute_in_registration_order_before_handler_and_reverse_after()
    {
        var events = new List<string>();
        var handler = new EventCommandHandler(events);

        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(events);
            services.AddSingleton<ICommandHandler<EventCommand, Unit>>(handler);
            services.AddScoped<IPipelineBehavior<EventCommand, Unit>>(
                _ => new OrderedBehavior<EventCommand, Unit>(events, "A"));
            services.AddScoped<IPipelineBehavior<EventCommand, Unit>>(
                _ => new OrderedBehavior<EventCommand, Unit>(events, "B"));
        });

        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new EventCommand());

        Assert.Equal(["A-before", "B-before", "handler", "B-after", "A-after"], events);
    }

    [Fact]
    public async Task Short_circuit_skips_handler_and_inner_behaviors()
    {
        var events = new List<string>();

        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>, PingCommandHandler>();
            services.AddScoped<IPipelineBehavior<PingCommand, string>>(
                _ => new ShortCircuitBehavior<PingCommand, string>("short"));
            services.AddScoped<IPipelineBehavior<PingCommand, string>>(
                _ => new OrderedBehavior<PingCommand, string>(events, "inner"));
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PingCommand, string>>();

        var result = await dispatcher.SendAsync(new PingCommand("ignored"));

        Assert.Equal("short", result);
        Assert.Empty(events);
        Assert.Equal(0, ((PingCommandHandler)handler).Calls);
    }

    [Fact]
    public async Task Handler_exceptions_propagate()
    {
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<ICommandHandler<ThrowingCommand, Unit>, ThrowingCommandHandler>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.SendAsync(new ThrowingCommand()));

        Assert.Equal("handler-failed", exception.Message);
    }

    [Fact]
    public async Task Behavior_exceptions_propagate()
    {
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>, PingCommandHandler>();
            services.AddScoped<IPipelineBehavior<PingCommand, string>, ThrowingBehavior<PingCommand, string>>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.SendAsync(new PingCommand("x")));
    }

    [Fact]
    public async Task Cancellation_token_is_passed_to_behaviors_and_handlers()
    {
        using var cts = new CancellationTokenSource();
        var seen = new List<CancellationToken>();

        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(seen);
            services.AddSingleton<ICommandHandler<TokenCommand, Unit>, TokenCommandHandler>();
            services.AddScoped<IPipelineBehavior<TokenCommand, Unit>, TokenBehavior<TokenCommand, Unit>>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        await dispatcher.SendAsync(new TokenCommand(), cts.Token);

        Assert.Equal(2, seen.Count);
        Assert.All(seen, token => Assert.Equal(cts.Token, token));
    }

    [Fact]
    public async Task Canceled_token_throws_before_handler_resolution()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.SendAsync(new PingCommand("x"), cts.Token));
    }

    private sealed record EventCommand : ICommand<Unit>;

    private sealed class EventCommandHandler(List<string> events) : ICommandHandler<EventCommand, Unit>
    {
        public Task<Unit> HandleAsync(EventCommand command, CancellationToken cancellationToken)
        {
            events.Add("handler");
            return Task.FromResult(Unit.Value);
        }
    }

    private sealed class OrderedBehavior<TRequest, TResponse>(List<string> events, string name)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            events.Add($"{name}-before");
            var response = await next();
            events.Add($"{name}-after");
            return response;
        }
    }

    private sealed class ShortCircuitBehavior<TRequest, TResponse>(TResponse response)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }

    private sealed class ThrowingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("behavior-failed");
    }

    private sealed record TokenCommand : ICommand<Unit>;

    private sealed class TokenCommandHandler(List<CancellationToken> seen) : ICommandHandler<TokenCommand, Unit>
    {
        public Task<Unit> HandleAsync(TokenCommand command, CancellationToken cancellationToken)
        {
            seen.Add(cancellationToken);
            return Task.FromResult(Unit.Value);
        }
    }

    private sealed class TokenBehavior<TRequest, TResponse>(List<CancellationToken> seen)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public Task<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            seen.Add(cancellationToken);
            return next();
        }
    }
}
