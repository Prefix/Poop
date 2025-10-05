using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Interfaces.Modules.Player;
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
    private readonly IPlayerManager _playerManager;
    private readonly IClientListenerManager _clientListenerManager;

    public DeadPlayerTracker(
        ILogger<DeadPlayerTracker> logger,
        IEventManager eventManager,
        IClientListenerManager clientListenerManager,
        IPlayerManager playerManager)
    {
        _logger = logger;
        _playerManager = playerManager;
        _clientListenerManager = clientListenerManager;

        // Register event listeners
        eventManager.ListenEvent("player_death", OnPlayerDeath);
        eventManager.ListenEvent("round_start", OnRoundStart);

        // Subscribe to client events
        _clientListenerManager.ClientDisconnected += OnClientDisconnected;
    }

    public bool Init()
    {
        _logger.LogInformation("DeadPlayerTracker initialized");
        return true;
    }

    public void OnAllSharpModulesLoaded()
    {
        _logger.LogDebug("DeadPlayerTracker: All modules loaded");
    }

    public void Shutdown()
    {
        _logger.LogInformation("DeadPlayerTracker shutting down");
        
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
            _logger.LogDebug("Player death event: victim controller is null or invalid");
            return;
        }

        try
        {
            var pawn = victim.GetPlayerPawn();
            if (pawn != null && pawn.IsValid())
            {
                var vec = pawn.GetAbsOrigin();

                IGameClient? gameClient = _playerManager.GetPlayer(victim.PlayerSlot)?.Client;
                if (gameClient == null)
                {
                    _logger.LogDebug("Player death event: could not find game client for slot {slot}", victim.PlayerSlot);
                    return;
                }
                _deadPlayers[gameClient] = new DeadPlayerInfo(vec, gameClient);

                _logger.LogDebug("Tracked dead player: {name} (slot {slot}, SteamID: {steamId}) at ({x:F2}, {y:F2}, {z:F2})",
                    victim.PlayerName, victim.PlayerSlot, gameClient.SteamId.ToString(), vec.X, vec.Y, vec.Z);
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
        var count = _deadPlayers.Count;
        _deadPlayers.Clear();
        _logger.LogDebug("Round start: Cleared {count} dead player(s)", count);
    }

    /// <summary>
    /// Handles client disconnect - removes player from tracking if found
    /// </summary>
    private void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (_deadPlayers.Remove(client))
        {
            _logger.LogDebug("Removed dead player tracking for disconnected client: {name} (SteamID: {steamId}, Reason: {reason})",
                client.Name, client.SteamId.ToString(), reason);
        }
    }
}
