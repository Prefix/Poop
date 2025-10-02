using Prefix.Poop.Interfaces;
using Prefix.Poop.Managers.Event;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Types;

namespace Prefix.Poop.Modules.EventExample;

internal sealed class BlockEvent : IModule
{
    private readonly ILogger<BlockEvent> _logger;

    public BlockEvent(ILogger<BlockEvent> logger, IEventManager eventManager)
    {
        _logger = logger;

        eventManager.HookEvent("player_changename", OnPlayerChangeName);
        eventManager.HookEvent("weapon_reload",     OnWeaponReload);
    }

    public bool Init()
        => true;

    private HookReturnValue<bool> OnPlayerChangeName(EventHookParams param)
    {
        // [[unlikely]] this should never happen
        if (param.Event is not IEventPlayerChangeName e)
        {
            return new HookReturnValue<bool>();
        }

        _logger.LogInformation("{old} changed name to {new}", e.OldName, e.NewName);

        // block event fire
        return new HookReturnValue<bool>(EHookAction.SkipCallReturnOverride);
    }

    private HookReturnValue<bool> OnWeaponReload(EventHookParams param)
    {
        // make server only,
        // don't broadcast to client
        param.SetServerOnly();

        return new HookReturnValue<bool>();
    }
}
