using System;
using Prefix.Poop.Interfaces;
using Prefix.Poop.PoopAPI;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.SharedInterfaceExample;

internal sealed class SharedInterface : IModule, IPoopAPI
{
    private readonly ILogger<SharedInterface> _logger;
    private readonly InterfaceBridge          _bridge;

    public SharedInterface(ILogger<SharedInterface> logger, InterfaceBridge bridge)
    {
        _logger = logger;
        _bridge = bridge;
    }

    public bool Init()
        => true;

    public void OnPostInit()
    {
        _bridge.SharpModuleManager.RegisterDynamicNative(_bridge.Poop, $"{IPoopAPI.Identity}.Hi",    Hi);
        _bridge.SharpModuleManager.RegisterDynamicNative(_bridge.Poop, $"{IPoopAPI.Identity}.Hello", Hello);

        _bridge.SharpModuleManager.RegisterSharpModuleInterface(_bridge.Poop, IPoopAPI.Identity, this);
    }

    private int Hi(int echo)
    {
        _logger.LogInformation("Dynamic Invoke: Hi({arg})\n{stack}", echo, Environment.StackTrace);

        return echo * echo;
    }

    public void Hello(IGameClient client)
    {
        if (_bridge.EntityManager.FindPlayerControllerBySlot(client.Slot) is not { } controller)
        {
            return;
        }

        controller.EmitSoundClient("Player.BecomeGhost");
    }

    public void Idle(IPlayerController controller)
    {
        if (controller.Team <= CStrikeTeam.Spectator || controller.GetPawn() is not { } pawn)
        {
            return;
        }

        // pawn is observer
        if (!pawn.IsPlayer())
        {
            return;
        }

        controller.ChangeTeam(CStrikeTeam.Spectator);
    }

    public void Kick(IPlayerPawn pawn)
    {
        if (pawn.GetOriginalController() is not { } controller)
        {
            return;
        }

        if (_bridge.ModSharp.GetIServer().GetGameClient(controller.PlayerSlot) is not { SignOnState: SignOnState.Full } client)
        {
            return;
        }

        _logger.LogInformation("Kick client {s}<{id}>", client.Name, client.SteamId);

        _bridge.ClientManager.KickClient(client, "Kick", NetworkDisconnectionReason.Kicked);
    }
}
