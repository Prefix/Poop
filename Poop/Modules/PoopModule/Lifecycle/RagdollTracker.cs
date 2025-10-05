using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopModule.Lifecycle;

/// <summary>
/// Tracks ragdoll entities spawned when players die, indexed by game client
/// Subscribes to EntityListenerManager to track ragdoll spawns and deletions
/// Parses ragdoll entity names to extract player slots and resolves them to IGameClient for consistent tracking
/// </summary>
internal sealed class RagdollTracker : IRagdollTracker
{
    private readonly ILogger<RagdollTracker> _logger;
    private readonly IEntityListenerManager _entityListenerManager;
    private readonly IClientListenerManager _clientListenerManager;
    private readonly IPlayerManager _playerManager;
    private readonly Dictionary<IGameClient, RagdollInfo> _ragdolls = new();

    public IReadOnlyDictionary<IGameClient, RagdollInfo> Ragdolls => _ragdolls;

    public RagdollTracker(
        ILogger<RagdollTracker> logger,
        IEntityListenerManager entityListenerManager,
        IClientListenerManager clientListenerManager,
        IEventManager eventManager,
        IPlayerManager playerManager)
    {
        _logger = logger;
        _entityListenerManager = entityListenerManager;
        _clientListenerManager = clientListenerManager;
        _playerManager = playerManager;

        // Subscribe to entity events
        _entityListenerManager.EntityCreated += OnEntityCreated;
        _entityListenerManager.EntityDeleted += OnEntityDeleted;

        // Subscribe to client events
        _clientListenerManager.ClientDisconnected += OnClientDisconnected;

        // Register event listeners for round management
        eventManager.ListenEvent("round_start", OnRoundStart);
    }
    public bool Init()
    {
        _logger.LogInformation("RagdollTracker initialized - subscribed to EntityListenerManager and ClientListenerManager");
        return true;
    }

    public void OnAllSharpModulesLoaded()
    {
        _logger.LogDebug("RagdollTracker: All modules loaded");
    }

    public void Shutdown()
    {
        _logger.LogInformation("RagdollTracker shutting down");

        // Unsubscribe from entity events
        _entityListenerManager.EntityCreated -= OnEntityCreated;
        _entityListenerManager.EntityDeleted -= OnEntityDeleted;

        // Unsubscribe from client events
        _clientListenerManager.ClientDisconnected -= OnClientDisconnected;

        _ragdolls.Clear();
    }

    /// <summary>
    /// Called when an entity is created - tracks ragdoll entities by parsing their name and resolving to game client
    /// Ragdoll names follow pattern: ragdoll_[role]_[slot] (e.g., "ragdoll_traitor_12")
    /// Parses the player slot from the name and resolves it to an IGameClient for tracking
    /// </summary>
    private void OnEntityCreated(IBaseEntity entity)
    {
        if (!entity.IsValid())
            return;

        try
        {
            // Get entity name
            var entityName = entity.Name;
            if (string.IsNullOrEmpty(entityName))
                return;

            // Check if this is a ragdoll (starts with "ragdoll_")
            if (!entityName.StartsWith("ragdoll_", StringComparison.OrdinalIgnoreCase))
                return;

            // Parse the name: ragdoll_[role]_[slot]
            // Example: "ragdoll_traitor_12" -> slot = 12
            var parts = entityName.Split('_');
            if (parts.Length < 3)
            {
                _logger.LogDebug("Ragdoll name '{name}' doesn't match expected pattern", entityName);
                return;
            }

            // The last part should be the player slot
            var slotStr = parts[^1];
            if (!int.TryParse(slotStr, out int playerSlot))
            {
                _logger.LogDebug("Failed to parse player slot from ragdoll name '{name}'", entityName);
                return;
            }

            // Get the game client for this player slot
            var gameClient = _playerManager.GetPlayer(playerSlot)?.Client;
            if (gameClient == null)
            {
                _logger.LogDebug("Ragdoll entity '{name}': could not find game client for slot {slot}", entityName, playerSlot);
                return;
            }

            // Store ragdoll by game client
            _ragdolls[gameClient] = new RagdollInfo(entity, gameClient);

            var position = entity.GetAbsOrigin();
            _logger.LogDebug("Tracked ragdoll '{name}' for {clientName} (slot {slot}, SteamID: {steamId}) at ({x:F2}, {y:F2}, {z:F2})",
                entityName, gameClient.Name, playerSlot, gameClient.SteamId.ToString(), position.X, position.Y, position.Z);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking ragdoll entity");
        }
    }

    /// <summary>
    /// Called when an entity is deleted - removes ragdoll from tracking by finding the associated game client
    /// </summary>
    private void OnEntityDeleted(IBaseEntity entity)
    {
        if (!entity.IsValid())
            return;

        // Try to find and remove the ragdoll from our dictionary
        IGameClient? clientToRemove = null;
        foreach (var kvp in _ragdolls)
        {
            if (kvp.Value.Ragdoll == entity)
            {
                clientToRemove = kvp.Key;
                break;
            }
        }

        if (clientToRemove != null && _ragdolls.Remove(clientToRemove))
        {
            _logger.LogDebug("Removed ragdoll for client {name} (SteamID: {steamId}) from tracking", 
                clientToRemove.Name, clientToRemove.SteamId.ToString());
        }
    }

    /// <summary>
    /// Handles round start - clears all tracked ragdolls
    /// </summary>
    private void OnRoundStart(IGameEvent ev)
    {
        var count = _ragdolls.Count;
        _ragdolls.Clear();
        _logger.LogDebug("Round start: Cleared {count} ragdoll(s)", count);
    }

    /// <summary>
    /// Handles client disconnect - removes ragdoll tracking for disconnected player
    /// </summary>
    private void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (_ragdolls.Remove(client))
        {
            _logger.LogDebug("Removed ragdoll tracking for disconnected client: {name} (SteamID: {steamId}, Reason: {reason})",
                client.Name, client.SteamId.ToString(), reason);
        }
    }
}
