using Conductor.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.Testing;

/// <summary>
/// Builds a scope-validated service provider for Conductor.NET tests.
/// </summary>
public sealed class ConductorTestHost : IDisposable
{
    private ConductorTestHost(ServiceProvider provider)
    {
        Provider = provider;
    }

    /// <summary>
    /// Gets the root provider created with scope validation enabled.
    /// </summary>
    public ServiceProvider Provider { get; }

    /// <summary>
    /// Creates a host using the supplied service configuration.
    /// </summary>
    /// <param name="configure">Registers Conductor.NET and test doubles.</param>
    /// <returns>A disposable test host.</returns>
    public static ConductorTestHost Create(Action<IServiceCollection> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var services = new ServiceCollection();
        configure(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        return new ConductorTestHost(provider);
    }

    /// <summary>
    /// Creates a host that already includes <c>AddConductor</c>.
    /// </summary>
    /// <param name="configureConductor">Conductor options.</param>
    /// <param name="configureServices">Additional service registration.</param>
    /// <returns>A disposable test host.</returns>
    public static ConductorTestHost Create(
        Action<ConductorOptions> configureConductor,
        Action<IServiceCollection>? configureServices = null)
    {
        ArgumentNullException.ThrowIfNull(configureConductor);

        return Create(services =>
        {
            services.AddConductor(configureConductor);
            configureServices?.Invoke(services);
        });
    }

    /// <summary>
    /// Creates a validated dependency-injection scope.
    /// </summary>
    /// <returns>The created scope.</returns>
    public IServiceScope CreateScope() => Provider.CreateScope();

    /// <inheritdoc />
    public void Dispose() => Provider.Dispose();
}
