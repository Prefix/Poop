using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Database;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Interfaces.Modules.Player;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Shared.Models;
using Prefix.Poop.Utils;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;
using Vector = Sharp.Shared.Types.Vector;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Handles spawning of poop entities with physics, colors, and size variations
/// </summary>
internal sealed class PoopSpawner(
    ILogger<PoopSpawner> logger,
    InterfaceBridge bridge,
    IDeadPlayerTracker deadPlayerTracker,
    IRagdollTracker ragdollTracker,
    IPoopLifecycleManager lifecycleManager,
    IRainbowPoopTracker rainbowTracker,
    IConfigManager config,
    ILocaleManager locale,
    IPoopDatabase database,
    IPlayerManager playerManager)
    : IPoopSpawner
{
    private readonly Random _random = new();

    /// <summary>
    /// Event fired after a poop spawn attempt (success or failure)
    /// </summary>
    public event Action<PoopSpawnedInternalEventArgs>? PoopSpawnedInternal;

    /// <summary>
    /// Spawns a poop entity at the specified position
    /// </summary>
    /// <param name="position">World position to spawn the poop</param>
    /// <param name="size">Size multiplier (-1 for random)</param>
    /// <param name="color">Optional color override</param>
    /// <param name="victimName">Name of the victim being pooped on</param>
    /// <returns>Spawn result with entity, size, position, and victim info</returns>
    public SpawnPoopResult SpawnPoop(Vector position, float size = -1.0f, PoopColorPreference? color = null, string? victimName = null)
    {
        try
        {
            // Create the physics prop entity as IBaseModelEntity so we can set RenderColor for rainbow mode
            var entity = bridge.EntityManager.CreateEntityByName<IBaseModelEntity>("prop_physics");
            if (entity == null)
            {
                logger.LogError("Failed to create poop entity (CreateEntityByName returned null)");
                return new SpawnPoopResult { Entity = null, Size = 0, Position = position, VictimName = victimName };
            }
            // Configure spawn flags
            entity.SpawnFlags = (uint)(
                SpawnFlags.SF_PHYSPROP_DEBRIS |       // Don't collide with players
                SpawnFlags.SF_PHYSPROP_TOUCH |        // Can be crashed through
                SpawnFlags.SF_PHYSPROP_FORCE_TOUCH_TRIGGERS
            );

            // Determine size
            float poopSize = size > 0 ? size : GetRandomPoopSize();
            float massFactor = poopSize * 0.05f;

            // Get color for spawn
            var poopColor = GetPoopColor(color);

            // Spawn the entity with all properties set via key values
            logger.LogInformation("Spawning poop model -> {_config.PoopModel}", config.PoopModel);
            entity.DispatchSpawn(
                new Dictionary<string, KeyValuesVariantValueItem>
                {
                    {"model", config.PoopModel},
                    {"spawnflags", entity.SpawnFlags},
                    {"origin", $"{position.X} {position.Y} {position.Z}"},
                    {"scale", poopSize},
                    {"massscale", massFactor},
                    {"inertiascale", massFactor},
                    {"rendercolor", $"{poopColor.Red} {poopColor.Green} {poopColor.Blue}"}
                }
            );
            entity.SetCollisionGroup(CollisionGroupType.InteractiveDebris);
            entity.CollisionRulesChanged();

            logger.LogDebug("Spawned poop at ({x}, {y}, {z}) with size {size}",
                position.X, position.Y, position.Z, poopSize);

            // Track poop for lifecycle management (lifetime, cleanup, etc.)
            lifecycleManager.TrackPoop(entity);

            // Track rainbow poops for color cycling
            if (color?.IsRainbow == true)
            {
                rainbowTracker.TrackRainbowPoop(entity);
                logger.LogDebug("Tracked rainbow poop entity for color cycling");
            }

            return new SpawnPoopResult
            {
                Entity = entity,
                Size = poopSize,
                Position = position,
                VictimName = victimName
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error spawning poop entity");
            return new SpawnPoopResult { Entity = null, Size = 0, Position = position, VictimName = victimName };
        }
    }

    /// <summary>
    /// Gets the color for the poop entity based on configuration and player preferences
    /// </summary>
    private PoopColorPreference GetPoopColor(PoopColorPreference? colorOverride)
    {
        if (colorOverride != null)
        {
            return colorOverride;
        }

        // No color override provided, use default color from config
        // Note: Player preferences are now handled in PoopCommands before calling SpawnPoop()
        var (r, g, b) = config.GetDefaultColorRgb();
        return new PoopColorPreference(r, g, b);
    }

    /// <summary>
    /// Generates a random poop size based on rarity configuration
    /// </summary>
    public float GetRandomPoopSize()
    {
        // Use configured default size
        float defaultSize = config.DefaultPoopSize;
        float minSize = config.MinPoopSize;
        float maxSize = config.MaxPoopSize;

        // Get chance percentages
        int commonChance = config.CommonSizeChance;
        int smallChance = config.SmallSizeChance;
        int unused = config.RareSizeChance;

        int roll = _random.Next(1, 101); // Roll 1-100

        float sizeValue;
        string sizeCategory;

        if (roll <= commonChance) // Common sizes (85% default)
        {
            // Normal sizes around default (0.9 - 1.1 of default)
            sizeCategory = "Common";
            sizeValue = defaultSize * (0.9f + (float)(_random.NextDouble() * 0.2f));
        }
        else if (roll <= commonChance + smallChance) // Small sizes (10% default)
        {
            // Small sizes (min to 0.9 of default)
            sizeCategory = "Small";
            float range = (defaultSize * 0.9f) - minSize;
            sizeValue = minSize + (float)(_random.NextDouble() * range);
        }
        else // Rare sizes (5% default)
        {
            // Large sizes (1.1 of default to max)
            sizeCategory = "Rare";
            float range = maxSize - (defaultSize * 1.1f);
            sizeValue = (defaultSize * 1.1f) + (float)(_random.NextDouble() * range);
        }

        // Clamp to configured min/max
        sizeValue = Math.Clamp(sizeValue, minSize, maxSize);

        // Round to 3 decimal places
        sizeValue = MathF.Round(sizeValue * 1000) / 1000;

        logger.LogDebug("Generated {category} poop with size {size:F3}", sizeCategory, sizeValue);

        return sizeValue;
    }

    /// <summary>
    /// Finds the nearest dead player to a position
    /// </summary>
    /// <param name="position">Position to search from</param>
    /// <param name="pooperSlot">Slot of the player doing the pooping (to exclude)</param>
    /// <returns>Information about the nearest dead player or null</returns>
    public DeadPlayerInfo? FindNearestDeadPlayer(Vector position, int pooperSlot)
    {
        // Check config to determine which detection method to use
        if (config.UseRagdollVictimDetection)
        {
            return FindNearestDeadPlayerRagdoll(position, pooperSlot);
        }
        else
        {
            return FindNearestDeadPlayerTraditional(position, pooperSlot);
        }
    }

    /// <summary>
    /// Finds nearest dead player using ragdoll entity detection
    /// </summary>
    private DeadPlayerInfo? FindNearestDeadPlayerRagdoll(Vector position, int pooperSlot)
    {
        try
        {
            DeadPlayerInfo? closest = null;
            float closestDistSq = float.MaxValue; // Distance to closest ragdoll found so far
            float maxDistSq = config.RagdollDetectionDistance * config.RagdollDetectionDistance; // Max search radius

            // Iterate through all tracked ragdolls
            foreach (var entry in ragdollTracker.Ragdolls)
            {
                // Skip the pooper themselves
                if (entry.Key == pooperSlot)
                    continue;

                var ragdoll = entry.Value;

                // Validate ragdoll
                if (!ragdoll.IsValid())
                    continue;

                // Get ragdoll position
                var ragdollPos = ragdoll.GetAbsOrigin();

                // Calculate distance squared from player to this ragdoll (faster than distance)
                float distSq = position.DistanceSquared(ragdollPos);

                // Check if within max range AND closer than current closest
                if (distSq < maxDistSq && distSq < closestDistSq)
                {
                    // Get player name from dead player tracker (same slot)
                    string playerName = "Unknown Player";
                    if (deadPlayerTracker.DeadPlayers.TryGetValue(entry.Key, out var deadPlayerInfo))
                    {
                        playerName = deadPlayerInfo.PlayerName;
                    }

                    closest = new DeadPlayerInfo(ragdollPos, playerName);
                    closestDistSq = distSq;

                    logger.LogDebug("Found ragdoll at distance {dist:F2} (name: {name})",
                        MathF.Sqrt(distSq), playerName);
                }
            }

            if (closest != null)
            {
                logger.LogInformation("Ragdoll detection: Found nearest dead player '{name}' at distance {dist:F2}",
                    closest.PlayerName, MathF.Sqrt(closestDistSq));
            }
            else
            {
                logger.LogDebug("Ragdoll detection: No ragdolls found within {maxDist} units (tracked: {count})",
                    config.RagdollDetectionDistance, ragdollTracker.Ragdolls.Count);
            }

            return closest;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ragdoll detection for nearest dead player");
            return null;
        }
    }

    /// <summary>
    /// Finds nearest dead player using traditional tracked dead player positions
    /// </summary>
    private DeadPlayerInfo? FindNearestDeadPlayerTraditional(Vector position, int pooperSlot)
    {
        try
        {
            DeadPlayerInfo? closest = null;
            float closestDistSq = float.MaxValue; // Distance to closest dead player found so far
            float maxDistSq = config.MaxDeadPlayerDistance * config.MaxDeadPlayerDistance; // Max search radius

            foreach (var entry in deadPlayerTracker.DeadPlayers)
            {
                // Skip the pooper themselves
                if (entry.Key == pooperSlot)
                    continue;

                // Calculate distance squared from player to this dead player
                float distSq = position.DistanceSquared(entry.Value.Position);

                // Check if within max range AND closer than current closest
                if (distSq < maxDistSq && distSq < closestDistSq)
                {
                    closestDistSq = distSq;
                    closest = entry.Value;
                }
            }

            if (closest != null)
            {
                logger.LogInformation("Traditional tracking: Found nearest dead player '{name}' at distance {dist:F2}",
                    closest.PlayerName, MathF.Sqrt(closestDistSq));
            }
            else
            {
                logger.LogDebug("Traditional tracking: No dead players found within {maxDist} units (tracked: {count})",
                    config.MaxDeadPlayerDistance, deadPlayerTracker.DeadPlayers.Count);
            }

            return closest;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in traditional tracking for nearest dead player");
            return null;
        }
    }

    /// <summary>
    /// Spawns a poop with complete logic including sounds, messages, and database logging
    /// Must be called from the main game thread
    /// </summary>
    public SpawnPoopResult SpawnPoopWithFullLogic(
        string playerSteamId,
        Vector position,
        float size,
        PoopColorPreference colorPreference,
        string? victimName = null,
        string? victimSteamId = null,
        bool playSounds = true,
        bool showMessages = true)
    {
        var playerName = playerManager.GetPlayerBySteamId(playerSteamId)?.Name ?? "Unknown";

        // Spawn the poop using the low-level spawner
        var result = SpawnPoop(position, size, colorPreference, victimName);

        if (result.Entity == null)
        {
            logger.LogError("Failed to spawn poop for {player}", playerName);
            
            // Fire internal event for failed spawn
            PoopSpawnedInternal?.Invoke(new PoopSpawnedInternalEventArgs
            {
                PlayerSteamId = playerSteamId,
                Position = position,
                Size = size,
                VictimName = victimName,
                VictimSteamId = victimSteamId,
                Success = false
            });
            
            return result;
        }

        float poopSize = result.Size;
        string sizeDesc = GetSizeDescription(poopSize);

        // Fire internal event for successful spawn
        PoopSpawnedInternal?.Invoke(new PoopSpawnedInternalEventArgs
        {
            PlayerSteamId = playerSteamId,
            Position = position,
            Size = poopSize,
            VictimName = victimName,
            VictimSteamId = victimSteamId,
            Success = true
        });

        // Play sound
        if (playSounds && config is { EnableSounds: true, PoopSounds.Length: > 0 })
        {
            var soundIndex = _random.Next(config.PoopSounds.Length);
            var soundEvent = config.PoopSounds[soundIndex];
            bridge.SoundManager.StartSoundEvent(soundEvent, result.Entity, config.SoundVolume);
        }

        // Show messages
        if (showMessages && config.ShowMessageOnPoop)
        {
            // Special announcement for MASSIVE poops (size >= 2.0)
            if (poopSize >= 2.0f)
            {
                bridge.ModSharp.PrintToChatAll(
                    ControllerExtensions.FormatChatMessage(locale.GetString("poop.spawned_massive", new Dictionary<string, object>
                    {
                        ["playerName"] = playerName,
                        ["sizeDesc"] = sizeDesc,
                        ["size"] = poopSize
                    })));
            }
            else if (victimName != null)
            {
                // Announce to all players in chat with size info
                bridge.ModSharp.PrintToChatAll(
                    ControllerExtensions.FormatChatMessage(locale.GetString("poop.spawned_on_player", new Dictionary<string, object>
                    {
                        ["playerName"] = playerName,
                        ["victimName"] = victimName,
                        ["sizeDesc"] = sizeDesc,
                        ["size"] = poopSize
                    })));
            }
            else
            {
                // No victim - spawn at player's position, show size to player only
                IGamePlayer? player = playerManager.GetPlayerBySteamId(playerSteamId);
                if (player != null && player.IsValid())
                {
                    IPlayerController? controller = player.Controller;
                    controller?.PrintToChat(locale.GetString("poop.spawned_self", new Dictionary<string, object>
                    {
                        ["sizeDesc"] = sizeDesc,
                        ["size"] = poopSize
                    }));
                }
            }
        }

        // Save to database (background thread)
        var currentMap = bridge.ModSharp.GetMapName() ?? "unknown";
        Task.Run(async () =>
        {
            try
            {
                var logRecord = new PoopLogRecord
                {
                    PlayerName = playerName,
                    PlayerSteamId = playerSteamId,
                    TargetName = victimName,
                    TargetSteamId = victimSteamId,
                    MapName = currentMap,
                    PoopSize = poopSize,
                    PoopColorR = colorPreference.Red,
                    PoopColorG = colorPreference.Green,
                    PoopColorB = colorPreference.Blue,
                    IsRainbow = colorPreference.IsRainbow,
                    PlayerX = position.X,
                    PlayerY = position.Y,
                    PlayerZ = position.Z,
                    Timestamp = DateTime.UtcNow
                };

                var logId = await database.LogPoopAsync(logRecord);
                logger.LogDebug("Logged poop event #{id} for {player}", logId, playerName);

                // If there was a victim, display victim count
                if (!string.IsNullOrEmpty(victimSteamId) && victimName != null)
                {
                    int victimCount = await database.GetVictimPoopCountAsync(victimSteamId);

                    if (victimCount > 0)
                    {
                        await bridge.ModSharp.InvokeFrameActionAsync(() =>
                        {
                            bridge.ModSharp.PrintToChatAll(
                                ControllerExtensions.FormatChatMessage(locale.GetString("leaderboard.victim_total_count", new Dictionary<string, object>
                                {
                                    ["victimName"] = victimName,
                                    ["count"] = victimCount
                                })));
                        });
                    }
                }
            }
            catch (Exception dbEx)
            {
                logger.LogError(dbEx, "Failed to save poop stats to database for {player}", playerName);
            }
        });

        return result;
    }

    /// <summary>
    /// Gets a descriptive name for a poop size with color codes using localization
    /// </summary>
    private string GetSizeDescription(float size)
    {
        if (size >= 2.5f) return locale.GetString("size.legendary");
        if (size >= 2.0f) return locale.GetString("size.desc_massive");
        if (size >= 1.7f) return locale.GetString("size.desc_huge");
        if (size >= 1.4f) return locale.GetString("size.desc_large");
        if (size >= 1.1f) return locale.GetString("size.desc_above_average");
        if (size >= 0.9f) return locale.GetString("size.desc_normal");
        if (size >= 0.7f) return locale.GetString("size.desc_small");
        if (size >= 0.5f) return locale.GetString("size.desc_tiny");
        return locale.GetString("size.desc_microscopic");
    }

    public bool Init()
    {
        return true;
    }
}
