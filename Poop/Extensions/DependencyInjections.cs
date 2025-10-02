using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Prefix.Poop.Extensions;

internal static class DependencyInjections
{
    public static IServiceCollection AddLogging(this IServiceCollection services, ILoggerFactory factory)
    {
        services.AddSingleton(factory);
        services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));

        return services;
    }

    public static IServiceCollection AddSingleton<TService1, TService2, T>(this IServiceCollection services)
        where T : class, TService1, TService2
        where TService1 : class
        where TService2 : class
    {
        services.AddSingleton<T>();
        services.AddSingleton<TService1, T>(x => x.GetRequiredService<T>());
        services.AddSingleton<TService2, T>(x => x.GetRequiredService<T>());

        return services;
    }

    public static IServiceCollection AddSingleton<TService1, TService2, TService3, T>(this IServiceCollection services)
        where T : class, TService1, TService2, TService3
        where TService1 : class
        where TService2 : class
        where TService3 : class
    {
        services.AddSingleton<T>();
        services.AddSingleton<TService1, T>(x => x.GetRequiredService<T>());
        services.AddSingleton<TService2, T>(x => x.GetRequiredService<T>());
        services.AddSingleton<TService3, T>(x => x.GetRequiredService<T>());

        return services;
    }

    public static IServiceCollection ImplSingleton<TService1, TService2, TImpl>(this IServiceCollection services)
        where TImpl : class, TService1, TService2
        where TService1 : class
        where TService2 : class
    {
        services.AddSingleton<TService1>(x => x.GetRequiredService<TImpl>());
        services.AddSingleton<TService2>(x => x.GetRequiredService<TImpl>());

        return services;
    }
}
