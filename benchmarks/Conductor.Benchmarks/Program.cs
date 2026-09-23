using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Conductor;
using Conductor.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<DispatchBenchmarks>();

[MemoryDiagnoser]
public class DispatchBenchmarks
{
    private PingHandler _handler = null!;
    private IServiceScope _scope = null!;
    private ICommandDispatcher _dispatcher = null!;
    private ICommandDispatcher _dispatcherWithBehaviors = null!;
    private PingCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        _handler = new PingHandler();
        _command = new PingCommand();

        _scope = Build(options => options.AddValidationBehavior = false).CreateScope();
        _dispatcher = _scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        var withBehaviors = Build(options =>
        {
            options.AddValidationBehavior = true;
            options.AddBehavior(typeof(NoOpBehavior<,>));
            options.AddBehavior(typeof(NoOpBehavior2<,>));
            options.AddBehavior(typeof(NoOpBehavior3<,>));
        }).CreateScope();
        _dispatcherWithBehaviors = withBehaviors.ServiceProvider.GetRequiredService<ICommandDispatcher>();

        _dispatcher.SendAsync(_command).GetAwaiter().GetResult();
        _dispatcherWithBehaviors.SendAsync(_command).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true)]
    public Task<Unit> DirectHandler() => _handler.HandleAsync(_command, CancellationToken.None);

    [Benchmark]
    public Task<Unit> DispatchNoBehaviors() => _dispatcher.SendAsync(_command);

    [Benchmark]
    public Task<Unit> DispatchWithThreeBehaviorsAndValidation() =>
        _dispatcherWithBehaviors.SendAsync(_command);

    private static ServiceProvider Build(Action<ConductorOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddConductor(configure);
        services.AddSingleton<ICommandHandler<PingCommand, Unit>, PingHandler>();
        return services.BuildServiceProvider();
    }
}

public sealed record PingCommand : ICommand<Unit>;

public sealed class PingHandler : ICommandHandler<PingCommand, Unit>
{
    public Task<Unit> HandleAsync(PingCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Unit.Value);
}

public sealed class NoOpBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        next();
}

public sealed class NoOpBehavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        next();
}

public sealed class NoOpBehavior3<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        next();
}
