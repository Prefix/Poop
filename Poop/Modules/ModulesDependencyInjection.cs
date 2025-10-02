using Prefix.Poop.Interfaces;
using Prefix.Poop.Modules.EventExample;
//using Prefix.Poop.Modules.HookExample;
using Prefix.Poop.Modules.SharedInterfaceExample;
using Prefix.Poop.Modules.PoopModule;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Prefix.Poop.Modules;

internal static class ModulesDependencyInjection
{
    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        // Register Poop Module Configuration
        services.AddSingleton<PoopModuleConfig>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var config = new PoopModuleConfig();
            configuration.GetSection("PoopModule").Bind(config);
            config.Validate(); // Validate and fix any invalid values
            return config;
        });

        return services
           //.AddSingleton<IModule, BlockEvent>()
           //.AddSingleton<IModule, ListenEvent>()
           //.AddSingleton<IModule, ReplaceEvent>()
           //.AddSingleton<IModule, FilterCrashFixes>()
           .AddSingleton<IModule, SharedInterface>()
           // Poop Module - Main game event handlers and command system
           .AddSingleton<IModule, PoopModule.PoopModule>()
           .AddSingleton<IModule, PoopCommands>()
           // Poop Module - Database service
           .AddSingleton<IPoopDatabase, PoopDatabase>();
    }
}
