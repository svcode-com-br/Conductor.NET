using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.AspNetCore;

/// <summary>
/// Registers ASP.NET Core ProblemDetails mapping for Conductor.NET validation failures.
/// </summary>
public static class ConductorAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds ProblemDetails and an exception handler for <see cref="RequestValidationException"/>.
    /// The application must also call <c>UseExceptionHandler</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddConductorProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddExceptionHandler<RequestValidationExceptionHandler>();
        services.AddProblemDetails();
        return services;
    }
}
