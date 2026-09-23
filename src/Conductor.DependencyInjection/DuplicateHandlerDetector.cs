using Conductor;
using Microsoft.Extensions.DependencyInjection;

namespace Conductor.DependencyInjection;

internal static class DuplicateHandlerDetector
{
    public static void ThrowIfDuplicates(IServiceCollection services)
    {
        ThrowIfDuplicates(services, typeof(ICommandHandler<,>));
        ThrowIfDuplicates(services, typeof(IQueryHandler<,>));
    }

    private static void ThrowIfDuplicates(IServiceCollection services, Type openHandlerType)
    {
        var groups = services
            .Where(descriptor =>
                descriptor.ServiceType.IsGenericType &&
                descriptor.ServiceType.GetGenericTypeDefinition() == openHandlerType)
            .GroupBy(descriptor => descriptor.ServiceType);

        foreach (var group in groups)
        {
            var implementations = group
                .Select(descriptor => descriptor.ImplementationType)
                .OfType<Type>()
                .Distinct()
                .ToArray();

            if (implementations.Length <= 1)
            {
                continue;
            }

            var requestType = group.Key.GenericTypeArguments[0];
            throw new DuplicateHandlerException(requestType, implementations);
        }
    }
}
