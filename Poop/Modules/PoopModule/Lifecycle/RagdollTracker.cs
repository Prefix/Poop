using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopModule.Lifecycle;

/// <summary>
/// Tracks ragdoll entities spawned when players die
/// Subscribes to EntityListenerManager to track ragdoll spawns and deletions
/// </summary>
internal sealed class RagdollTracker : IRagdollTracker, Interfaces.Modules.IRagdollTracker
{
    private readonly ILogger<RagdollTracker> _logger;
    private readonly IEntityListenerManager _entityListenerManager;
    private readonly Dictionary<int, IBaseEntity> _ragdolls = new(); // Key: PlayerSlot, Value: Ragdoll entity

    public IReadOnlyDictionary<int, IBaseEntity> Ragdolls => _ragdolls;

    public RagdollTracker(
        ILogger<RagdollTracker> logger,
        IEntityListenerManager entityListenerManager,
        IEventManager eventManager)
    {
        _logger = logger;
        _entityListenerManager = entityListenerManager;

        // Subscribe to entity events
        _entityListenerManager.EntityCreated += OnEntityCreated;
        _entityListenerManager.EntityDeleted += OnEntityDeleted;

        // Register event listeners for round management
        eventManager.ListenEvent("round_start", OnRoundStart);
    }
    public bool Init()
    {
        _logger.LogInformation("RagdollTracker initialized - subscribed to EntityListenerManager");
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

        _ragdolls.Clear();
    }

    /// <summary>
    /// Called when an entity is created - tracks ragdoll entities by parsing their name
    /// Ragdoll names follow pattern: ragdoll_[role]_[slot] (e.g., "ragdoll_traitor_12")
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
            var slotStr = parts[parts.Length - 1];
            if (!int.TryParse(slotStr, out int playerSlot))
            {
                _logger.LogDebug("Failed to parse player slot from ragdoll name '{name}'", entityName);
                return;
            }

            // Store ragdoll by player slot
            _ragdolls[playerSlot] = entity;

            var position = entity.GetAbsOrigin();
            _logger.LogDebug("Tracked ragdoll '{name}' for slot {slot} at ({x:F2}, {y:F2}, {z:F2})",
                entityName, playerSlot, position.X, position.Y, position.Z);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking ragdoll entity");
        }
    }

    /// <summary>
    /// Called when an entity is deleted - removes ragdoll from tracking
    /// </summary>
    private void OnEntityDeleted(IBaseEntity entity)
    {
        if (!entity.IsValid())
            return;

        // Try to find and remove the ragdoll from our dictionary
        // Since we're storing by player slot, we need to find which slot this entity belongs to
        int? slotToRemove = null;
        foreach (var kvp in _ragdolls)
        {
            if (kvp.Value == entity)
            {
                slotToRemove = kvp.Key;
                break;
            }
        }

        if (slotToRemove.HasValue && _ragdolls.Remove(slotToRemove.Value))
        {
            _logger.LogDebug("Removed ragdoll for player slot {slot} from tracking", slotToRemove.Value);
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
}
