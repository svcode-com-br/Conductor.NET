using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.DependencyInjection;

/// <summary>
/// Registers Conductor.NET dispatchers, the notification publisher, handlers, validators, and pipeline behaviors.
/// </summary>
public static class ConductorServiceCollectionExtensions
{
    /// <summary>
    /// Adds Conductor.NET with default options. Handlers may be registered manually.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddConductor(this IServiceCollection services) =>
        AddConductor(services, _ => { });

    /// <summary>
    /// Adds Conductor.NET and optionally scans the assemblies supplied in <paramref name="configure"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Options that control lifetimes, scanning, and pipeline behaviors.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddConductor(
        this IServiceCollection services,
        Action<ConductorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ConductorOptions();
        configure(options);
        ValidateOptions(options);

        services.Add(new ServiceDescriptor(
            typeof(ICommandDispatcher),
            typeof(CommandDispatcher),
            options.DispatcherLifetime));

        services.Add(new ServiceDescriptor(
            typeof(IQueryDispatcher),
            typeof(QueryDispatcher),
            options.DispatcherLifetime));

        services.Add(new ServiceDescriptor(
            typeof(INotificationPublisher),
            typeof(NotificationPublisher),
            options.DispatcherLifetime));

        services.Add(new ServiceDescriptor(
            typeof(IMediator),
            typeof(Mediator),
            options.DispatcherLifetime));

        foreach (var behaviorType in options.Behaviors)
        {
            services.Add(new ServiceDescriptor(
                typeof(IPipelineBehavior<,>),
                behaviorType,
                ServiceLifetime.Scoped));
        }

        AssemblyScanner.RegisterDiscoveredTypes(services, options);

        if (options.AddValidationBehavior)
        {
            services.Add(new ServiceDescriptor(
                typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>),
                ServiceLifetime.Scoped));
        }

        if (options.ThrowOnDuplicateHandlers)
        {
            DuplicateHandlerDetector.ThrowIfDuplicates(services);
        }

        return services;
    }

    private static void ValidateOptions(ConductorOptions options)
    {
        if (!IsSupportedLifetime(options.DispatcherLifetime) ||
            !IsSupportedLifetime(options.HandlerLifetime) ||
            !IsSupportedLifetime(options.ValidatorLifetime))
        {
            throw new InvalidOperationException(
                "Dispatcher, handler, and validator lifetimes must be Singleton, Scoped, or Transient.");
        }
    }

    private static bool IsSupportedLifetime(ServiceLifetime lifetime) =>
        lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped or ServiceLifetime.Transient;
}
