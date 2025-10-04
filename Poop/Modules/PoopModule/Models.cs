using System;
using System.Collections.Generic;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Types;
using Vector = Sharp.Shared.Types.Vector;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Stores information about a dead player for poop placement
/// </summary>
public sealed class DeadPlayerInfo
{
    public Vector Position { get; }
    public string PlayerName { get; }
    public string? SteamId { get; }
    public DateTime DeathTime { get; }

    public DeadPlayerInfo(Vector position, string playerName, string? steamId = null)
    {
        Position = position;
        PlayerName = playerName;
        SteamId = steamId;
        DeathTime = DateTime.UtcNow;
    }
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
    /// Chance for common size (85%)
    /// </summary>
    public int CommonSizeChance { get; set; } = 85;

    /// <summary>
    /// Chance for small size (10%)
    /// </summary>
    public int SmallSizeChance { get; set; } = 10;

    /// <summary>
    /// Chance for rare/large size (5%)
    /// Calculated as: 100 - CommonSizeChance - SmallSizeChance
    /// </summary>
    public int RareSizeChance => 100 - CommonSizeChance - SmallSizeChance;
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
    public string SteamId { get; set; } = string.Empty;
    public int PoopCount { get; set; }
}

/// <summary>
/// Top victim record (aggregated from poop_logs)
/// </summary>
internal sealed class TopVictimRecord
{
    public string Name { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public int VictimCount { get; set; }
}

/// <summary>
/// Tracks a rainbow poop entity
/// </summary>
internal sealed class RainbowPoopInfo
{
    public int EntityIndex { get; set; }
    public float CurrentHue { get; set; }
    public DateTime SpawnTime { get; set; }

    public RainbowPoopInfo(int entityIndex)
    {
        EntityIndex = entityIndex;
        CurrentHue = 0.0f;
        SpawnTime = DateTime.UtcNow;
    }
}

/// <summary>
/// Command cooldown tracker
/// </summary>
internal sealed class CommandCooldownTracker
{
    private readonly Dictionary<string, Dictionary<ulong, DateTime>> _cooldowns = new();
    private readonly int _cooldownSeconds;

    public CommandCooldownTracker(int cooldownSeconds = 3)
    {
        _cooldownSeconds = cooldownSeconds;
    }

    public bool CanExecute(string commandName, ulong steamId)
    {
        if (!_cooldowns.ContainsKey(commandName))
        {
            _cooldowns[commandName] = new Dictionary<ulong, DateTime>();
        }

        var commandCooldowns = _cooldowns[commandName];

        if (commandCooldowns.TryGetValue(steamId, out var lastUse))
        {
            var timeSinceLastUse = DateTime.UtcNow - lastUse;
            if (timeSinceLastUse.TotalSeconds < _cooldownSeconds)
            {
                return false;
            }
        }

        commandCooldowns[steamId] = DateTime.UtcNow;
        return true;
    }

    public void Clear()
    {
        _cooldowns.Clear();
    }

    public void ClearPlayer(ulong steamId)
    {
        foreach (var commandCooldowns in _cooldowns.Values)
        {
            commandCooldowns.Remove(steamId);
        }
    }

    public double GetRemainingCooldown(string commandName, ulong steamId)
    {
        if (!_cooldowns.TryGetValue(commandName, out var commandCooldowns))
        {
            return 0;
        }

        if (!commandCooldowns.TryGetValue(steamId, out var lastUse))
        {
            return 0;
        }

        var timeSinceLastUse = DateTime.UtcNow - lastUse;
        var remaining = _cooldownSeconds - timeSinceLastUse.TotalSeconds;

        return Math.Max(0, remaining);
    }
}
