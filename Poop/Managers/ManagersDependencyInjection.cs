using Microsoft.Extensions.DependencyInjection;
using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Managers.ClientListener;
using Prefix.Poop.Managers.Commands;
using Prefix.Poop.Managers.EntityListener;
using Prefix.Poop.Managers.Event;
using Prefix.Poop.Managers.GameListener;
using Prefix.Poop.Managers.Hook;
using Prefix.Poop.Managers.Menu;
using Prefix.Poop.Managers.Player;

namespace Prefix.Poop.Managers;

internal static class ManagersDependencyInjection
{
    public static IServiceCollection AddManagers(this IServiceCollection services)
    {
        services.AddSingleton<IManager, IConfigManager, ConfigManager>();
        services.AddSingleton<IManager, ILocaleManager, LocaleManager>();
        services.AddSingleton<IManager, IHookManager, HookManager>();
        services.AddSingleton<IManager, IEventManager, EventManager>();
        services.AddSingleton<IManager, IClientListenerManager, ClientListenerManager>();
        services.AddSingleton<IManager, IGameListenerManager, GameListenerManager>();
        services.AddSingleton<IManager, IEntityListenerManager, EntityListenerManager>();
        services.AddSingleton<IManager, IPlayerManager, PlayerManager>();
        services.AddSingleton<IManager, ICommandManager, CommandManager>();
        services.AddSingleton<IManager, IMenuManager, MenuManager>();
        return services;
    }
}
