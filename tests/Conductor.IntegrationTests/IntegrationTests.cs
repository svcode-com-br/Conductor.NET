using System.Diagnostics;
using Conductor;
using Conductor.DependencyInjection;
using Conductor.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.IntegrationTests;

public sealed class IntegrationTests
{
    [Fact]
    public async Task Open_generic_behaviors_and_multiple_validators_run_in_a_real_scope()
    {
        var context = new RecordingContext();

        using var host = ConductorTestHost.Create(options =>
        {
            options.AddBehavior(typeof(IntegrationRecordingBehavior<,>));
            options.RegisterServicesFromAssemblyContaining<IntegrationCommand>();
        }, services =>
        {
            services.AddSingleton(context);
        });

        using var scope = host.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => mediator.SendAsync(new IntegrationCommand("")));

        Assert.NotEmpty(exception.Failures);
        Assert.Contains("Recording-before", context.Events);
        Assert.DoesNotContain("Recording-after", context.Events);
    }

    [Fact]
    public async Task Activity_is_created_for_successful_command_dispatch()
    {
        var started = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ConductorTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => started.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        using var host = ConductorTestHost.Create(_ => { }, services =>
        {
            services.AddScoped<ICommandHandler<IntegrationCommand, string>, IntegrationCommandHandler>();
        });

        using var scope = host.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.SendAsync(new IntegrationCommand("ok"));

        Assert.Equal("ok", result);
        var activity = Assert.Single(
            started,
            item =>
                item.OperationName == ConductorTelemetry.CommandActivityName &&
                Equals(item.GetTagItem(ConductorTelemetry.RequestTypeTag), typeof(IntegrationCommand).FullName));
        Assert.Equal("command", activity.GetTagItem(ConductorTelemetry.RequestKindTag));
        Assert.Equal(ConductorTelemetry.OutcomeSuccess, activity.GetTagItem(ConductorTelemetry.OutcomeTag));
        Assert.Equal(typeof(IntegrationCommandHandler).FullName, activity.GetTagItem(ConductorTelemetry.HandlerTypeTag));
    }

    [Fact]
    public async Task Cancellation_is_not_converted_to_validation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        using var host = ConductorTestHost.Create(_ => { }, services =>
        {
            services.AddScoped<ICommandHandler<IntegrationCommand, string>, IntegrationCommandHandler>();
        });

        using var scope = host.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.SendAsync(new IntegrationCommand("ok"), cts.Token));
    }
}

public sealed record IntegrationCommand(string Name) : ICommand<string>;

public sealed class IntegrationCommandHandler : ICommandHandler<IntegrationCommand, string>
{
    public Task<string> HandleAsync(IntegrationCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(command.Name);
}

public sealed class IntegrationCommandValidator : IValidator<IntegrationCommand>
{
    public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
        IntegrationCommand request,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ValidationFailure> failures = string.IsNullOrWhiteSpace(request.Name)
            ?
            [
                new ValidationFailure(nameof(request.Name), "Name.Required", "Name is required.")
            ]
            : [];

        return ValueTask.FromResult(failures);
    }
}

public sealed class IntegrationRecordingBehavior<TRequest, TResponse>(RecordingContext context)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        context.Add("Recording-before");
        var response = await next().ConfigureAwait(false);
        context.Add("Recording-after");
        return response;
    }
}
