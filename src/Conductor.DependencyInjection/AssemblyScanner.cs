using System.Reflection;
using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.DependencyInjection;

internal static class AssemblyScanner
{
    public static void RegisterDiscoveredTypes(IServiceCollection services, ConductorOptions options)
    {
        foreach (var assembly in options.Assemblies)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }

                foreach (var iface in type.GetInterfaces())
                {
                    if (!iface.IsGenericType)
                    {
                        continue;
                    }

                    var definition = iface.GetGenericTypeDefinition();
                    if (definition == typeof(ICommandHandler<,>) ||
                        definition == typeof(IQueryHandler<,>) ||
                        definition == typeof(INotificationHandler<>))
                    {
                        services.Add(new ServiceDescriptor(iface, type, options.HandlerLifetime));
                    }
                    else if (definition == typeof(IValidator<>))
                    {
                        services.Add(new ServiceDescriptor(iface, type, options.ValidatorLifetime));
                    }
                }
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
