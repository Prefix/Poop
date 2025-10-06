using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopModule.Lifecycle;

/// <summary>
/// Tracks dead player positions for traditional (non-ragdoll) detection
/// Listens to game events and maintains a dictionary of dead player locations
/// </summary>
internal sealed class DeadPlayerTracker : IDeadPlayerTracker
{
    private readonly ILogger<DeadPlayerTracker> _logger;
    private readonly Dictionary<IGameClient, DeadPlayerInfo> _deadPlayers = new();

    public IReadOnlyDictionary<IGameClient, DeadPlayerInfo> DeadPlayers => _deadPlayers;
    private readonly IClientListenerManager _clientListenerManager;

    public DeadPlayerTracker(
        ILogger<DeadPlayerTracker> logger,
        IEventManager eventManager,
        IClientListenerManager clientListenerManager)
    {
        _logger = logger;
        _clientListenerManager = clientListenerManager;

        // Register event listeners
        eventManager.ListenEvent("player_death", OnPlayerDeath);
        eventManager.ListenEvent("round_start", OnRoundStart);

        // Subscribe to client events
        _clientListenerManager.ClientDisconnected += OnClientDisconnected;
    }

    public bool Init()
    {
        return true;
    }

    public void Shutdown()
    {
        // Unsubscribe from client events
        _clientListenerManager.ClientDisconnected -= OnClientDisconnected;
        
        _deadPlayers.Clear();
    }

    /// <summary>
    /// Handles player death events - stores dead player position
    /// </summary>
    private void OnPlayerDeath(IGameEvent ev)
    {
        if (ev is not IEventPlayerDeath e)
        {
            return;
        }

        var victim = e.VictimController;
        if (victim == null || !victim.IsValid())
        {
            return;
        }

        try
        {
            var pawn = victim.GetPlayerPawn();
            if (pawn != null && pawn.IsValid())
            {
                var vec = pawn.GetAbsOrigin();

                IGameClient? gameClient = victim.GetGameClient();
                if (gameClient == null)
                {
                    return;
                }
                _deadPlayers[gameClient] = new DeadPlayerInfo(vec, gameClient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking dead player {name}", victim.PlayerName);
        }
    }

    /// <summary>
    /// Handles round start - clears all tracked dead players
    /// </summary>
    private void OnRoundStart(IGameEvent ev)
    {
        _deadPlayers.Clear();
    }

    /// <summary>
    /// Handles client disconnect - removes player from tracking if found
    /// </summary>
    private void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        _deadPlayers.Remove(client);
    }
}
