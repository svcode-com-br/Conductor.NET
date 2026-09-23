using System.Reflection;
using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.DependencyInjection;

/// <summary>
/// Configuration for Conductor.NET service registration.
/// </summary>
public sealed class ConductorOptions
{
    internal List<Assembly> Assemblies { get; } = [];

    internal List<Type> Behaviors { get; } = [];

    /// <summary>
    /// Gets or sets the lifetime used for dispatchers and the mediator. The default is scoped.
    /// </summary>
    public ServiceLifetime DispatcherLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets the lifetime used for discovered handlers. The default is scoped.
    /// </summary>
    public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets the lifetime used for discovered validators. The default is scoped.
    /// </summary>
    public ServiceLifetime ValidatorLifetime { get; set; } = ServiceLifetime.Scoped;

    /// <summary>
    /// Gets or sets a value indicating whether the built-in validation behavior is registered.
    /// The default is <see langword="true"/>.
    /// </summary>
    public bool AddValidationBehavior { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether duplicate closed handlers fail registration.
    /// The default is <see langword="true"/>.
    /// </summary>
    public bool ThrowOnDuplicateHandlers { get; set; } = true;

    /// <summary>
    /// Adds an assembly to scan for handlers and validators.
    /// </summary>
    /// <param name="assembly">The assembly to scan. Scanning never inspects assemblies that were not supplied.</param>
    /// <returns>The same options instance.</returns>
    public ConductorOptions RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!Assemblies.Contains(assembly))
        {
            Assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>
    /// Adds the assembly that contains <typeparamref name="T"/> to the scan list.
    /// </summary>
    /// <typeparam name="T">A type whose assembly should be scanned.</typeparam>
    /// <returns>The same options instance.</returns>
    public ConductorOptions RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Registers an open-generic pipeline behavior. Behaviors are registered in the order they are added;
    /// the first behavior is the outermost and executes first.
    /// </summary>
    /// <param name="openGenericBehaviorType">An open generic type such as <c>LoggingBehavior&lt;,&gt;</c>.</param>
    /// <returns>The same options instance.</returns>
    public ConductorOptions AddBehavior(Type openGenericBehaviorType)
    {
        ArgumentNullException.ThrowIfNull(openGenericBehaviorType);

        if (!openGenericBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                "Behavior type must be an open generic type definition such as MyBehavior<,>.",
                nameof(openGenericBehaviorType));
        }

        if (openGenericBehaviorType.GetGenericArguments().Length != 2)
        {
            throw new ArgumentException(
                "Behavior type must have exactly two generic parameters.",
                nameof(openGenericBehaviorType));
        }

        if (!ImplementsPipelineBehavior(openGenericBehaviorType))
        {
            throw new ArgumentException(
                $"Type '{openGenericBehaviorType}' must implement {typeof(IPipelineBehavior<,>).FullName}.",
                nameof(openGenericBehaviorType));
        }

        Behaviors.Add(openGenericBehaviorType);
        return this;
    }

    private static bool ImplementsPipelineBehavior(Type type)
    {
        return type.GetInterfaces().Any(static iface =>
            iface.IsGenericType &&
            iface.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
    }
}
