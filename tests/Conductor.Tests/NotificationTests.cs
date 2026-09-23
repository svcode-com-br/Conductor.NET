using System.Diagnostics;
using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Tests;

public sealed class NotificationTests
{
    [Fact]
    public async Task PublishAsync_succeeds_when_no_handlers_are_registered()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        await publisher.PublishAsync(new OrderPlaced("none"));
    }

    [Fact]
    public async Task PublishAsync_invokes_handlers_in_registration_order()
    {
        var log = new HandlerLog();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton<INotificationHandler<OrderPlaced>, FirstOrderPlacedHandler>();
            services.AddSingleton<INotificationHandler<OrderPlaced>, SecondOrderPlacedHandler>();
        });

        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        await publisher.PublishAsync(new OrderPlaced("A"));

        Assert.Equal(["first", "second"], log.Entries);
    }

    [Fact]
    public async Task PublishAsync_stops_after_the_first_handler_exception()
    {
        var log = new HandlerLog();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton<INotificationHandler<OrderPlaced>, ThrowingOrderPlacedHandler>();
            services.AddSingleton<INotificationHandler<OrderPlaced>, SecondOrderPlacedHandler>();
        });

        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => publisher.PublishAsync(new OrderPlaced("fail")));

        Assert.Equal("notification-failed", exception.Message);
        Assert.Empty(log.Entries);
    }

    [Fact]
    public async Task PublishAsync_checks_cancellation_before_each_later_handler()
    {
        var log = new HandlerLog();
        using var cts = new CancellationTokenSource();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton(cts);
            services.AddSingleton<INotificationHandler<OrderPlaced>, CancellingOrderPlacedHandler>();
            services.AddSingleton<INotificationHandler<OrderPlaced>, SecondOrderPlacedHandler>();
        });

        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => publisher.PublishAsync(new OrderPlaced("cancel"), cts.Token));

        Assert.Equal(["cancelling"], log.Entries);
    }

    [Fact]
    public async Task PublishAsync_matches_the_runtime_type_only()
    {
        var log = new HandlerLog();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton<INotificationHandler<OrderPlaced>, FirstOrderPlacedHandler>();
            services.AddSingleton<INotificationHandler<IOrderEvent>, OrderEventHandler>();
        });

        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        INotification notification = new OrderPlaced("runtime");
        await publisher.PublishAsync(notification);

        Assert.Equal(["first"], log.Entries);
    }

    [Fact]
    public async Task PublishAsync_resolves_scoped_handlers_on_each_call()
    {
        var seen = new List<Guid>();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(seen);
            services.AddScoped<ScopeToken>();
            services.AddScoped<INotificationHandler<OrderPlaced>, ScopedOrderPlacedHandler>();
        });

        using (var first = provider.CreateScope())
        {
            await first.ServiceProvider.GetRequiredService<INotificationPublisher>()
                .PublishAsync(new OrderPlaced("one"));
        }

        using (var second = provider.CreateScope())
        {
            await second.ServiceProvider.GetRequiredService<INotificationPublisher>()
                .PublishAsync(new OrderPlaced("two"));
        }

        Assert.Equal(2, seen.Distinct().Count());
    }

    [Fact]
    public async Task Mediator_delegates_publish_to_the_notification_publisher()
    {
        var log = new HandlerLog();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton<INotificationHandler<OrderPlaced>, FirstOrderPlacedHandler>();
        });

        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new OrderPlaced("via-mediator"));

        Assert.Equal(["first"], log.Entries);
    }

    [Fact]
    public async Task PublishAsync_rejects_null_notification()
    {
        await using var provider = TestServiceProvider.Build();
        using var scope = provider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => publisher.PublishAsync(null!));
    }

    [Fact]
    public async Task PublishAsync_records_activity_tags_for_success_and_failure()
    {
        var started = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ConductorTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (activity.OperationName == ConductorTelemetry.NotificationActivityName)
                {
                    started.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var log = new HandlerLog();
        await using var provider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton(log);
            services.AddSingleton<INotificationHandler<OrderPlaced>, FirstOrderPlacedHandler>();
            services.AddSingleton<INotificationHandler<OrderPlaced>, SecondOrderPlacedHandler>();
        });

        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<INotificationPublisher>()
                .PublishAsync(new OrderPlaced("ok"));
        }

        var success = Assert.Single(
            started,
            activity => activity.OperationName == ConductorTelemetry.NotificationActivityName);
        Assert.Equal(typeof(OrderPlaced).FullName, success.GetTagItem(ConductorTelemetry.RequestTypeTag));
        Assert.Equal("notification", success.GetTagItem(ConductorTelemetry.RequestKindTag));
        Assert.Equal(ConductorTelemetry.OutcomeSuccess, success.GetTagItem(ConductorTelemetry.OutcomeTag));
        Assert.Equal(2, success.GetTagItem(ConductorTelemetry.HandlerCountTag));
        Assert.Null(success.GetTagItem(ConductorTelemetry.HandlerTypeTag));

        started.Clear();
        await using var failingProvider = TestServiceProvider.Build(services =>
        {
            services.AddSingleton<INotificationHandler<OrderPlaced>, ThrowingOrderPlacedHandler>();
            services.AddSingleton<INotificationHandler<OrderPlaced>, IdleOrderPlacedHandler>();
        });

        using (var scope = failingProvider.CreateScope())
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                scope.ServiceProvider.GetRequiredService<INotificationPublisher>()
                    .PublishAsync(new OrderPlaced("bad")));
        }

        var failure = Assert.Single(
            started,
            activity => activity.OperationName == ConductorTelemetry.NotificationActivityName);
        Assert.Equal(ConductorTelemetry.OutcomeError, failure.GetTagItem(ConductorTelemetry.OutcomeTag));
        Assert.Equal(2, failure.GetTagItem(ConductorTelemetry.HandlerCountTag));
        Assert.Equal(
            typeof(ThrowingOrderPlacedHandler).FullName,
            failure.GetTagItem(ConductorTelemetry.HandlerTypeTag));
    }

    private sealed class HandlerLog
    {
        public List<string> Entries { get; } = [];
    }

    private sealed class ScopeToken
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class FirstOrderPlacedHandler(HandlerLog log) : INotificationHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Entries.Add("first");
            return Task.CompletedTask;
        }
    }

    private sealed class SecondOrderPlacedHandler(HandlerLog log) : INotificationHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Entries.Add("second");
            return Task.CompletedTask;
        }
    }

    private sealed class IdleOrderPlacedHandler : INotificationHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ThrowingOrderPlacedHandler : INotificationHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("notification-failed");
    }

    private sealed class CancellingOrderPlacedHandler(HandlerLog log, CancellationTokenSource cancellation)
        : INotificationHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
        {
            log.Entries.Add("cancelling");
            cancellation.Cancel();
            return Task.CompletedTask;
        }
    }

    private sealed class OrderEventHandler(HandlerLog log) : INotificationHandler<IOrderEvent>
    {
        public Task HandleAsync(IOrderEvent notification, CancellationToken cancellationToken)
        {
            log.Entries.Add("base");
            return Task.CompletedTask;
        }
    }

    private sealed class ScopedOrderPlacedHandler(ScopeToken token, List<Guid> seen) : INotificationHandler<OrderPlaced>
    {
        public Task HandleAsync(OrderPlaced notification, CancellationToken cancellationToken)
        {
            seen.Add(token.Id);
            return Task.CompletedTask;
        }
    }
}

internal interface IOrderEvent : INotification;

internal sealed record OrderPlaced(string Id) : IOrderEvent;
