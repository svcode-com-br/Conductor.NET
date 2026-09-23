using Conductor;
using Conductor.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Tests;

public sealed class ValidationTests
{
    [Fact]
    public async Task All_validators_run_and_failures_are_aggregated()
    {
        await using var provider = Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>, PingCommandHandler>();
            services.AddSingleton<IValidator<PingCommand>, FirstPingValidator>();
            services.AddSingleton<IValidator<PingCommand>, SecondPingValidator>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PingCommand, string>>();

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => dispatcher.SendAsync(new PingCommand("bad")));

        Assert.Equal(2, exception.Failures.Count);
        Assert.Equal(typeof(PingCommand), exception.RequestType);
        Assert.Equal(0, ((PingCommandHandler)handler).Calls);
    }

    [Fact]
    public async Task Handler_runs_when_no_validators_are_registered()
    {
        await using var provider = Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>, PingCommandHandler>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.SendAsync(new PingCommand("ok"));

        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handler_runs_when_validators_return_no_failures()
    {
        await using var provider = Build(services =>
        {
            services.AddSingleton<ICommandHandler<PingCommand, string>, PingCommandHandler>();
            services.AddSingleton<IValidator<PingCommand>, PassingPingValidator>();
        });

        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var result = await dispatcher.SendAsync(new PingCommand("ok"));

        Assert.Equal("ok", result);
    }

    private static ServiceProvider Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddConductor();
        configure(services);
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    private sealed class FirstPingValidator : IValidator<PingCommand>
    {
        public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
            PingCommand request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>(
            [
                new ValidationFailure(nameof(request.Message), "Message.First", "first")
            ]);
    }

    private sealed class SecondPingValidator : IValidator<PingCommand>
    {
        public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
            PingCommand request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>(
            [
                new ValidationFailure(nameof(request.Message), "Message.Second", "second")
            ]);
    }

    private sealed class PassingPingValidator : IValidator<PingCommand>
    {
        public ValueTask<IReadOnlyCollection<ValidationFailure>> ValidateAsync(
            PingCommand request,
            CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyCollection<ValidationFailure>>([]);
    }
}
