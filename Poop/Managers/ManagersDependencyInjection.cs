using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Managers.Event;
using Prefix.Poop.Managers.Hook;
using Microsoft.Extensions.DependencyInjection;

namespace Prefix.Poop.Managers;

internal static class ManagersDependencyInjection
{
    public static IServiceCollection AddManagers(this IServiceCollection services)
        => services
           .AddSingleton<IManager, IHookManager, HookManager>()
           .AddSingleton<IManager, IEventManager, EventManager>();
}
