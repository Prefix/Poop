using System;
using System.Collections.Generic;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;
using Vector = Sharp.Shared.Types.Vector;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Stores information about a dead player for poop placement
/// Used by DeadPlayerTracker and ragdoll detection to track death locations
/// </summary>
public sealed class DeadPlayerInfo(Vector position, IGameClient? player)
{
    /// <summary>
    /// The world position where the player died
    /// </summary>
    public Vector Position { get; } = position;
    
    /// <summary>
    /// The game client (player) associated with this death location
    /// </summary>
    public IGameClient? Player { get; set; } = player;
}

/// <summary>
/// Stores information about a ragdoll entity associated with a dead player
/// Used by RagdollTracker to maintain ragdoll-to-player associations for poop placement
/// </summary>
public sealed class RagdollInfo(IBaseEntity ragdoll, IGameClient? player)
{
    /// <summary>
    /// The ragdoll entity
    /// </summary>
    public IBaseEntity Ragdoll { get; } = ragdoll;
    
    /// <summary>
    /// The game client (player) who owns this ragdoll
    /// </summary>
    public IGameClient? Player { get; } = player;
    
    /// <summary>
    /// When the ragdoll was spawned (tracked)
    /// </summary>
    public DateTime SpawnTime { get; } = DateTime.UtcNow;
}

/// <summary>
/// Configuration for poop size generation
/// </summary>
internal sealed class PoopSizeConfig
{
    /// <summary>
    /// Minimum poop size (rare small poops)
    /// Default: 0.1 to 0.5
    /// </summary>
    public float MinPoopSize { get; set; } = 0.3f;

    /// <summary>
    /// Maximum poop size (rare large poops)
    /// Default: 1.5 to 3.0
    /// </summary>
    public float MaxPoopSize { get; set; } = 2.0f;

    /// <summary>
    /// Default poop size (common poops)
    /// Default: 0.5 to 1.5
    /// </summary>
    public float DefaultPoopSize { get; set; } = 1.0f;

    /// <summary>
    /// Generation tiers for size randomization
    /// Each tier has a chance percentage and size range multiplier
    /// </summary>
    public List<PoopSizeGenerationTier> GenerationTiers { get; set; } = new()
    {
        new() { Chance = 40, Name = "Normal", MinMultiplier = 0.9f, MaxMultiplier = 1.1f },
        new() { Chance = 25, Name = "Above Average", MinMultiplier = 1.1f, MaxMultiplier = 1.4f },
        new() { Chance = 15, Name = "Small", MinMultiplier = 0.7f, MaxMultiplier = 0.9f },
        new() { Chance = 10, Name = "Large", MinMultiplier = 1.4f, MaxMultiplier = 1.7f },
        new() { Chance = 5, Name = "Tiny", MinMultiplier = 0.5f, MaxMultiplier = 0.7f },
        new() { Chance = 3, Name = "Huge", MinMultiplier = 1.7f, MaxMultiplier = 2.0f },
        new() { Chance = 2, Name = "Rare", MinMultiplier = 2.0f, MaxMultiplier = 2.6f }
    };

    /// <summary>
    /// Dynamic size categories with thresholds and locale keys
    /// Categories are checked from top to bottom (largest to smallest)
    /// </summary>
    public List<PoopSizeCategory> SizeCategories { get; set; } = new()
    {
        new() { Threshold = 2.5f, LocaleKey = "size.legendary" },
        new() { Threshold = 2.0f, LocaleKey = "size.desc_massive" },
        new() { Threshold = 1.7f, LocaleKey = "size.desc_huge" },
        new() { Threshold = 1.4f, LocaleKey = "size.desc_large" },
        new() { Threshold = 1.1f, LocaleKey = "size.desc_above_average" },
        new() { Threshold = 0.9f, LocaleKey = "size.desc_normal" },
        new() { Threshold = 0.7f, LocaleKey = "size.desc_small" },
        new() { Threshold = 0.5f, LocaleKey = "size.desc_tiny" },
        new() { Threshold = 0.0f, LocaleKey = "size.desc_microscopic" }
    };

    /// <summary>
    /// Threshold for "massive" poop announcements (global chat message)
    /// </summary>
    public float MassiveAnnouncementThreshold { get; set; } = 2.0f;
}

/// <summary>
/// Defines a size category with its threshold and localization key
/// </summary>
internal sealed class PoopSizeCategory
{
    /// <summary>
    /// Minimum size for this category (inclusive)
    /// </summary>
    public float Threshold { get; set; }

    /// <summary>
    /// Localization key for this size category
    /// </summary>
    public string LocaleKey { get; set; } = string.Empty;
}

/// <summary>
/// Defines a generation tier for size randomization
/// </summary>
internal sealed class PoopSizeGenerationTier
{
    /// <summary>
    /// Chance percentage for this tier (0-100)
    /// </summary>
    public int Chance { get; set; }

    /// <summary>
    /// Name/category of this tier for logging
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum size multiplier (relative to DefaultPoopSize)
    /// </summary>
    public float MinMultiplier { get; set; }

    /// <summary>
    /// Maximum size multiplier (relative to DefaultPoopSize)
    /// </summary>
    public float MaxMultiplier { get; set; }

    /// <summary>
    /// Optional sub-tiers for weighted distribution within this tier's range
    /// If defined, uses weighted probability instead of uniform distribution
    /// Can be used for any tier - ultra-rare large sizes OR ultra-rare tiny sizes
    /// </summary>
    public List<PoopSizeSubTier>? SubTiers { get; set; }
}

/// <summary>
/// Defines a sub-tier within a generation tier for advanced rarity distribution
/// </summary>
internal sealed class PoopSizeSubTier
{
    /// <summary>
    /// Weight for this sub-tier (higher = more common)
    /// Total weights across all sub-tiers determine probability
    /// </summary>
    public int Weight { get; set; }

    /// <summary>
    /// Name of this sub-tier for logging
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Minimum percentage of parent tier's range (0.0 - 1.0)
    /// Example: 0.0 = start of parent range, 0.5 = middle, 1.0 = end
    /// </summary>
    public float MinRangePercent { get; set; }

    /// <summary>
    /// Maximum percentage of parent tier's range (0.0 - 1.0)
    /// </summary>
    public float MaxRangePercent { get; set; }
}

/// <summary>
/// Database record for individual poop placements (full event log)
/// </summary>
internal sealed class PoopLogRecord
{
    public int Id { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerSteamId { get; set; } = string.Empty;
    public string? TargetName { get; set; }
    public string? TargetSteamId { get; set; }
    public string MapName { get; set; } = string.Empty;
    public float PoopSize { get; set; }
    public int PoopColorR { get; set; }
    public int PoopColorG { get; set; }
    public int PoopColorB { get; set; }
    public bool IsRainbow { get; set; }
    public float PlayerX { get; set; }
    public float PlayerY { get; set; }
    public float PlayerZ { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Top pooper record (aggregated from poop_logs)
/// </summary>
internal sealed class TopPooperRecord
{
    public string Name { get; set; } = string.Empty;
    public SteamID SteamId { get; set; }
    public int PoopCount { get; set; }
}

/// <summary>
/// Top victim record (aggregated from poop_logs)
/// </summary>
internal sealed class TopVictimRecord
{
    public string Name { get; set; } = string.Empty;
    public SteamID SteamId { get; set; }
    public int VictimCount { get; set; }
}

/// <summary>
/// Tracks a rainbow poop entity
/// </summary>
internal sealed class RainbowPoopInfo
{
    public IBaseModelEntity PoopEntity { get; set; }
    public float CurrentHue { get; set; }
    public DateTime SpawnTime { get; set; }

    public RainbowPoopInfo(IBaseModelEntity poopEntity)
    {
        PoopEntity = poopEntity;
        CurrentHue = 0.0f;
        SpawnTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Command cooldown tracker
/// </summary>
internal sealed class CommandCooldownTracker(int cooldownSeconds = 3)
{
    private readonly Dictionary<string, Dictionary<IGameClient, DateTime>> _cooldowns = new();

    public bool CanExecute(string commandName, IGameClient player)
    {
        if (!_cooldowns.ContainsKey(commandName))
        {
            _cooldowns[commandName] = new Dictionary<IGameClient, DateTime>();
        }

        var commandCooldowns = _cooldowns[commandName];

        if (commandCooldowns.TryGetValue(player, out var lastUse))
        {
            var timeSinceLastUse = DateTime.UtcNow - lastUse;
            if (timeSinceLastUse.TotalSeconds < cooldownSeconds)
            {
                return false;
            }
        }

        commandCooldowns[player] = DateTime.UtcNow;
        return true;
    }

    public void Clear()
    {
        _cooldowns.Clear();
    }

    public void ClearPlayer(IGameClient player)
    {
        foreach (var commandCooldowns in _cooldowns.Values)
        {
            commandCooldowns.Remove(player);
        }
    }

    public double GetRemainingCooldown(string commandName, IGameClient player)
    {
        if (!_cooldowns.TryGetValue(commandName, out var commandCooldowns))
        {
            return 0;
        }

        if (!commandCooldowns.TryGetValue(player, out var lastUse))
        {
            return 0;
        }

        var timeSinceLastUse = DateTime.UtcNow - lastUse;
        var remaining = cooldownSeconds - timeSinceLastUse.TotalSeconds;

        return Math.Max(0, remaining);
    }
}
