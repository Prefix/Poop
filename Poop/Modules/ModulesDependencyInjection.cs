using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Modules.PoopModule;
using Prefix.Poop.Modules.FlashingHtmlHudFix;
using Prefix.Poop.Modules.PoopPlayer;
using Prefix.Poop.Modules.PoopModule.Lifecycle;
using Prefix.Poop.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Prefix.Poop.Shared;
using IRagdollTracker = Prefix.Poop.Interfaces.PoopModule.Lifecycle.IRagdollTracker;

namespace Prefix.Poop.Modules;

internal static class ModulesDependencyInjection
{
    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        services.AddSingleton<IModule, IPoopShared, SharedInterface.SharedInterface>();
        services.AddSingleton<IModule, PoopPrecache>();
        services.AddSingleton<IModule, PoopCommands>();
        services.AddSingleton<IModule, IPoopDatabase, PoopDatabase>();
        services.AddSingleton<IModule, IPoopSpawner, PoopSpawner>();
        services.AddSingleton<IModule, IPoopSizeGenerator, PoopSizeGenerator>();
        services.AddSingleton<IModule, IPoopPlayerManager, PoopPlayerManager>();
        services.AddSingleton<IModule, IPoopColorMenu, PoopColorMenu>();
        services.AddSingleton<IModule, IRainbowPoopTracker, RainbowPoopTracker>();
        services.AddSingleton<IModule, IDeadPlayerTracker, DeadPlayerTracker>();
        services.AddSingleton<IModule, IRagdollTracker, RagdollTracker>();
        services.AddSingleton<IModule, IPoopLifecycleManager, PoopLifecycleManager>();
        services.AddSingleton<IModule, FlashingHtmlHudFixModule>();
        return services;
    }
}
