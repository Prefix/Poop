using System;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Managers.Event;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Prefix.Poop.Modules.EventExample;

internal sealed class ReplaceEvent : IModule
{
    private readonly ILogger<ReplaceEvent> _logger;
    private readonly InterfaceBridge       _bridge;

    public ReplaceEvent(ILogger<ReplaceEvent> logger, InterfaceBridge bridge, IEventManager eventManager)
    {
        _logger = logger;
        _bridge = bridge;

        eventManager.HookEvent("player_death", OnPlayerDeath);
        eventManager.HookEvent("weapon_fire",  OnWeaponFire);
    }

    public bool Init()
        => true;

    private HookReturnValue<bool> OnPlayerDeath(EventHookParams param)
    {
        IGameEvent? clone = null;

        try
        {
            clone = _bridge.EventManager.CloneEvent(param.Event)
                    ?? throw new InvalidOperationException("Failed to clone event");

            // modify kill feed icon
            clone.SetString("weapon", "SawnLake");

            foreach (var client in _bridge.ModSharp.GetIServer().GetGameClients())
            {
                if (client is { SignOnState: SignOnState.Full, IsFakeClient: false })
                {
                    clone.FireToClient(client);
                }
            }

            // make it happens server only, we send dummy to clients
            param.SetServerOnly();

            return new HookReturnValue<bool>();
        }
        finally
        {
            clone?.Dispose();
        }
    }

    private HookReturnValue<bool> OnWeaponFire(EventHookParams param)
    {
        if (_bridge.EventManager.CloneEvent(param.Event) is not { } clone)
        {
            _logger.LogWarning("Failed to clone event {e}", param.Event.Name);

            return new HookReturnValue<bool>();
        }

        clone.SetString("weapon", "weapon_m4a1_silencer");
        clone.FireToClients();
        clone.Dispose();

        // after send clone, we make original event server only
        param.SetServerOnly();

        return new HookReturnValue<bool>();
    }
}
