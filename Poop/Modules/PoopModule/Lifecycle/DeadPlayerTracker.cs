using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopModule.Lifecycle;

/// <summary>
/// Tracks dead player positions for traditional (non-ragdoll) detection
/// Listens to game events and maintains a dictionary of dead player locations
/// </summary>
internal sealed class DeadPlayerTracker : IDeadPlayerTracker, Interfaces.Modules.IDeadPlayerTracker
{
    private readonly ILogger<DeadPlayerTracker> _logger;
    private readonly Dictionary<int, DeadPlayerInfo> _deadPlayers = new();

    public IReadOnlyDictionary<int, DeadPlayerInfo> DeadPlayers => _deadPlayers;

    public DeadPlayerTracker(ILogger<DeadPlayerTracker> logger, IEventManager eventManager)
    {
        _logger = logger;

        // Register event listeners
        eventManager.ListenEvent("player_death", OnPlayerDeath);
        eventManager.ListenEvent("round_start", OnRoundStart);
        eventManager.ListenEvent("player_disconnect", OnPlayerDisconnect);
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

                // Get victim's SteamID from the controller
                string? steamId = victim.SteamId.ToString();

                _deadPlayers[victim.PlayerSlot] = new DeadPlayerInfo(vec, victim.PlayerName, steamId);

                _logger.LogDebug("Tracked dead player: {name} (slot {slot}, SteamID: {steamId}) at ({x:F2}, {y:F2}, {z:F2})",
                    victim.PlayerName, victim.PlayerSlot, steamId ?? "unknown", vec.X, vec.Y, vec.Z);
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
    /// Handles player disconnect - removes player from tracking
    /// </summary>
    private void OnPlayerDisconnect(IGameEvent ev)
    {
        // Note: Player slot not available from disconnect event in current ModSharp API
        // Dead players are cleared on round start instead
        // This is acceptable behavior as disconnected players won't be targeted anyway
        _logger.LogDebug("Player disconnected (dead players cleared on round start)");
    }
}
