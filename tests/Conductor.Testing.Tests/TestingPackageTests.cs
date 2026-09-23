using Conductor;
using Conductor.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Testing.Tests;

public sealed class TestingPackageTests
{
    [Fact]
    public async Task Fake_dispatchers_record_and_return_configured_results()
    {
        var commands = new FakeCommandDispatcher()
            .On<Ping, string>((command, _) => Task.FromResult(command.Message));
        var queries = new FakeQueryDispatcher()
            .On<Lookup, int>((_, _) => Task.FromResult(9));
        var seen = new List<string>();
        var notifications = new FakeNotificationPublisher()
            .On<Pinged>((notification, _) =>
            {
                seen.Add(notification.Name);
                return Task.CompletedTask;
            });
        var mediator = new FakeMediator(commands, queries, notifications);

        Assert.Equal("hi", await mediator.SendAsync(new Ping("hi")));
        Assert.Equal(9, await mediator.QueryAsync(new Lookup()));
        await mediator.PublishAsync(new Pinged("order"));
        Assert.Single(commands.SentCommands);
        Assert.Single(queries.SentQueries);
        Assert.Equal(["order"], seen);
        Assert.Single(notifications.PublishedNotifications);
    }

    [Fact]
    public async Task Recording_behavior_and_pipeline_order_assertions_work()
    {
        var context = new RecordingContext();
        var counter = new HandlerInvocationCounter();

        using var host = ConductorTestHost.Create(options =>
        {
            options.AddValidationBehavior = false;
        }, services =>
        {
            services.AddSingleton(context);
            services.AddSingleton(counter);
            services.AddScoped<ICommandHandler<Ping, string>, CountingHandler>();
            services.AddRecordingBehavior<Ping, string>("A");
            services.AddRecordingBehavior<Ping, string>("B");
        });

        using var scope = host.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMediator>().SendAsync(new Ping("x"));

        PipelineOrderAssertions.Equal(
            ["A-before", "B-before", "B-after", "A-after"],
            context.Events);
        counter.ShouldHaveBeenCalled(1);
    }

    [Fact]
    public void Pipeline_order_mismatch_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            PipelineOrderAssertions.Equal(["a"], ["b"]));
    }

    private sealed record Ping(string Message) : ICommand<string>;

    private sealed record Lookup : IQuery<int>;

    private sealed record Pinged(string Name) : INotification;

    private sealed class CountingHandler(HandlerInvocationCounter counter) : ICommandHandler<Ping, string>
    {
        public Task<string> HandleAsync(Ping command, CancellationToken cancellationToken)
        {
            counter.Increment();
            return Task.FromResult(command.Message);
        }
    }
}
