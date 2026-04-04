using Microsoft.Extensions.DependencyInjection;

namespace Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHandlersFromAssemblyContaining<TMarker>(this IServiceCollection services)
        {
            var assembly = typeof(TMarker).Assembly;

            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name == "Handler")
                .ToList();
            foreach (var handlerType in handlerTypes)
            {
                services.AddScoped(handlerType);
            }

            return services;
        }
    }
}