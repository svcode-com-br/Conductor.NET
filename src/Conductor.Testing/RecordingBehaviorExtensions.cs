using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Testing;

/// <summary>
/// Registration helpers for recording pipeline behaviors.
/// </summary>
public static class RecordingBehaviorExtensions
{
    /// <summary>
    /// Registers a closed recording behavior that writes <c>{name}-before</c> and <c>{name}-after</c>.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The recording label.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddRecordingBehavior<TRequest, TResponse>(
        this IServiceCollection services,
        string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        services.AddScoped<IPipelineBehavior<TRequest, TResponse>>(provider =>
            new RecordingBehavior<TRequest, TResponse>(
                provider.GetRequiredService<RecordingContext>(),
                name));

        return services;
    }
}
