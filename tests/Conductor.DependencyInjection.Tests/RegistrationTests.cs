using Conductor;
using Conductor.DependencyInjection;
using Conductor.TestSupport.Duplicates;
using Conductor.TestSupport.Scan;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.DependencyInjection.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void AddConductor_registers_dispatchers_and_mediator()
    {
        var services = new ServiceCollection();
        services.AddConductor();

        Assert.Contains(services, d => d.ServiceType == typeof(ICommandDispatcher) && d.ImplementationType == typeof(CommandDispatcher));
        Assert.Contains(services, d => d.ServiceType == typeof(IQueryDispatcher) && d.ImplementationType == typeof(QueryDispatcher));
        Assert.Contains(services, d => d.ServiceType == typeof(INotificationPublisher) && d.ImplementationType == typeof(NotificationPublisher));
        Assert.Contains(services, d => d.ServiceType == typeof(IMediator) && d.ImplementationType == typeof(Mediator));
    }

    [Fact]
    public void AddConductor_does_not_require_assemblies_for_manual_registration()
    {
        var services = new ServiceCollection();
        services.AddConductor();
        services.AddScoped<ICommandHandler<ManualCommand, Unit>, ManualCommandHandler>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        using var scope = provider.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<ICommandHandler<ManualCommand, Unit>>());
    }

    [Fact]
    public void Scanning_discovers_handlers_and_validators_only_from_configured_assemblies()
    {
        var services = new ServiceCollection();
        services.AddConductor(options =>
        {
            options.RegisterServicesFromAssemblyContaining<ScannedCommand>();
        });

        Assert.Contains(services, d => d.ServiceType == typeof(ICommandHandler<ScannedCommand, string>));
        Assert.Contains(services, d => d.ServiceType == typeof(IQueryHandler<ScannedQuery, int>));
        Assert.Contains(services, d => d.ServiceType == typeof(IValidator<ScannedCommand>));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(INotificationHandler<ScannedNotification>) &&
            d.ImplementationType == typeof(FirstScannedNotificationHandler));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(INotificationHandler<ScannedNotification>) &&
            d.ImplementationType == typeof(SecondScannedNotificationHandler));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(AbstractCommandHandler));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(OpenGenericHandler<,>));
        Assert.DoesNotContain(services, d => d.ServiceType == typeof(ICommandHandler<DuplicateCommand, Unit>));
    }

    [Fact]
    public void Scanning_registers_multiple_validators_for_the_same_request()
    {
        var services = new ServiceCollection();
        services.AddConductor(options =>
        {
            options.RegisterServicesFromAssemblyContaining<ScannedCommand>();
        });

        var validators = services
            .Where(d => d.ServiceType == typeof(IValidator<ScannedCommand>))
            .Select(d => d.ImplementationType)
            .ToArray();

        Assert.Contains(typeof(ScannedCommandValidator), validators);
        Assert.Contains(typeof(SecondScannedCommandValidator), validators);
    }

    [Fact]
    public void Duplicate_command_handlers_fail_registration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<DuplicateHandlerException>(() =>
        {
            services.AddConductor(options =>
            {
                options.RegisterServicesFromAssemblyContaining<DuplicateCommand>();
            });
        });

        Assert.Equal(typeof(DuplicateCommand), exception.RequestType);
        Assert.Contains(typeof(DuplicateCommandHandlerA), exception.Handlers);
        Assert.Contains(typeof(DuplicateCommandHandlerB), exception.Handlers);
    }

    [Fact]
    public void Duplicate_query_handlers_fail_registration()
    {
        var services = new ServiceCollection();
        services.AddConductor(options => options.ThrowOnDuplicateHandlers = false);
        services.AddScoped<IQueryHandler<DuplicateQuery, int>, DuplicateQueryHandlerA>();
        services.AddScoped<IQueryHandler<DuplicateQuery, int>, DuplicateQueryHandlerB>();

        var exception = Assert.Throws<DuplicateHandlerException>(
            () => DuplicateHandlerDetector.ThrowIfDuplicates(services));

        Assert.Equal(typeof(DuplicateQuery), exception.RequestType);
        Assert.Contains(typeof(DuplicateQueryHandlerA), exception.Handlers);
        Assert.Contains(typeof(DuplicateQueryHandlerB), exception.Handlers);
    }

    [Fact]
    public void ThrowOnDuplicateHandlers_can_be_disabled()
    {
        var services = new ServiceCollection();
        services.AddConductor(options =>
        {
            options.ThrowOnDuplicateHandlers = false;
            options.RegisterServicesFromAssemblyContaining<DuplicateCommand>();
        });

        Assert.True(services.Count(d => d.ServiceType == typeof(ICommandHandler<DuplicateCommand, Unit>)) >= 2);
    }

    [Fact]
    public void Lifetimes_are_honored()
    {
        var services = new ServiceCollection();
        services.AddConductor(options =>
        {
            options.DispatcherLifetime = ServiceLifetime.Singleton;
            options.HandlerLifetime = ServiceLifetime.Transient;
            options.ValidatorLifetime = ServiceLifetime.Singleton;
            options.RegisterServicesFromAssemblyContaining<ScannedCommand>();
        });

        Assert.All(
            services.Where(d => d.ServiceType == typeof(IMediator) || d.ServiceType == typeof(INotificationPublisher)),
            d => Assert.Equal(ServiceLifetime.Singleton, d.Lifetime));
        Assert.Contains(services, d =>
            d.ServiceType == typeof(ICommandHandler<ScannedCommand, string>) &&
            d.Lifetime == ServiceLifetime.Transient);
        Assert.Contains(services, d =>
            d.ServiceType == typeof(INotificationHandler<ScannedNotification>) &&
            d.Lifetime == ServiceLifetime.Transient);
        Assert.Contains(services, d =>
            d.ServiceType == typeof(IValidator<ScannedCommand>) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddBehavior_registers_open_generic_before_validation()
    {
        var services = new ServiceCollection();
        services.AddConductor(options =>
        {
            options.AddBehavior(typeof(MarkerBehavior<,>));
        });

        var behaviors = services
            .Where(d => d.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(d => d.ImplementationType)
            .ToArray();

        Assert.Equal(typeof(MarkerBehavior<,>), behaviors[0]);
        Assert.Equal(typeof(ValidationBehavior<,>), behaviors[1]);
    }

    [Fact]
    public void AddBehavior_rejects_non_pipeline_types()
    {
        var options = new ConductorOptions();
        Assert.Throws<ArgumentException>(() => options.AddBehavior(typeof(ManualCommandHandler)));
    }
}

public sealed record ManualCommand : ICommand<Unit>;

public sealed class ManualCommandHandler : ICommandHandler<ManualCommand, Unit>
{
    public Task<Unit> HandleAsync(ManualCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Unit.Value);
}

public sealed class MarkerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        next();
}
