using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Prefix.Poop.Shared.Models;
using Prefix.Poop.Utils;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;
using IRagdollTracker = Prefix.Poop.Interfaces.PoopModule.Lifecycle.IRagdollTracker;
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
    IPoopSizeGenerator sizeGenerator,
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
    /// <returns>Spawn result with entity, size, position, and victim info</returns>
    public SpawnPoopResult SpawnPoop(Vector position, float size = -1.0f, PoopColorPreference? color = null)
    {
        try
        {
            // Create the physics prop entity as IBaseModelEntity so we can set RenderColor for rainbow mode
            var entity = bridge.EntityManager.CreateEntityByName<IBaseModelEntity>("prop_physics");
            if (entity == null)
            {
                return new SpawnPoopResult { Entity = null, Size = 0, Position = position };
            }
            // Configure spawn flags
            entity.SpawnFlags = (uint)(
                SpawnFlags.PhysPropDebris |       // Don't collide with players
                SpawnFlags.PhysPropTouch |        // Can be crashed through
                SpawnFlags.PhysPropForceTouchTriggers
            );

            // Determine size
            float poopSize = size > 0 ? size : sizeGenerator.GetRandomSize();
            float massFactor = poopSize * 0.05f;

            // Get color for spawn
            var poopColor = GetPoopColor(color);

            // Spawn the entity with all properties set via key values
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

            // Track poop for lifecycle management (lifetime, cleanup, etc.)
            lifecycleManager.TrackPoop(entity);

            // Track rainbow poops for color cycling
            if (color?.IsRainbow == true)
            {
                rainbowTracker.TrackRainbowPoop(entity);
            }

            return new SpawnPoopResult
            {
                Entity = entity,
                Size = poopSize,
                Position = position,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error spawning poop entity");
            return new SpawnPoopResult { Entity = null, Size = 0, Position = position };
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
    /// Finds the nearest dead player to a position
    /// </summary>
    /// <param name="pooper">The player doing the pooping (to exclude from search)</param>
    /// <returns>Information about the nearest dead player or null</returns>
    public DeadPlayerInfo? FindNearestDeadPlayer(IGameClient? pooper)
    {
        // Check config to determine which detection method to use
        if (config.UseRagdollVictimDetection)
        {
            return FindNearestDeadPlayerRagdoll(pooper);
        }
        else
        {
            return FindNearestDeadPlayerTraditional(pooper);
        }
    }

    /// <summary>
    /// Finds nearest dead player using ragdoll entity detection
    /// </summary>
    private DeadPlayerInfo? FindNearestDeadPlayerRagdoll(IGameClient? pooper)
    {
        if (pooper == null || !pooper.IsValid)
        {
            return null;
        }
        Vector position = playerManager.GetController(pooper)?.GetAbsOrigin() ?? new Vector(0, 0, 0);
        try
        {
            DeadPlayerInfo? closest = null;
            float closestDistSq = float.MaxValue; // Distance to closest ragdoll found so far
            float maxDistSq = config.RagdollDetectionDistance * config.RagdollDetectionDistance; // Max search radius

            // Iterate through all tracked ragdolls
            foreach (var entry in ragdollTracker.Ragdolls)
            {
                // Skip the pooper themselves
                if (entry.Key == pooper)
                    continue;

                var ragdollInfo = entry.Value;

                // Validate ragdoll
                if (!ragdollInfo.Ragdoll.IsValid())
                    continue;

                // Get ragdoll position
                var ragdollPos = ragdollInfo.Ragdoll.GetAbsOrigin();

                // Calculate distance squared from player to this ragdoll (faster than distance)
                float distSq = position.DistanceSquared(ragdollPos);

                // Check if within max range AND closer than current closest
                if (distSq < maxDistSq && distSq < closestDistSq)
                {
                    closest = new DeadPlayerInfo(ragdollPos, entry.Key);
                    closestDistSq = distSq;
                }
            }

            if (closest != null)
            {
                logger.LogInformation("Ragdoll detection: Found nearest dead player '{name}' at distance {dist:F2}",
                    closest.Player?.Name ?? "Unknown", MathF.Sqrt(closestDistSq));
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
    private DeadPlayerInfo? FindNearestDeadPlayerTraditional(IGameClient? pooper)
    {
        if (pooper == null || !pooper.IsValid)
        {
            return null;
        }
        IPlayerController? controller = playerManager.GetController(pooper);
        if (controller == null || !controller.IsValid())
        {
            return null;
        }
        IPlayerPawn? pawn = controller.GetPlayerPawn();
        if (pawn == null || !pawn.IsValid())
        {
            return null;
        }
        Vector position = pawn.GetAbsOrigin();
        try
        {
            DeadPlayerInfo? closest = null;
            float closestDistSq = float.MaxValue; // Distance to closest dead player found so far
            float maxDistSq = config.MaxDeadPlayerDistance * config.MaxDeadPlayerDistance; // Max search radius

            foreach (var entry in deadPlayerTracker.DeadPlayers)
            {
                // Skip the pooper themselves
                if (entry.Key == pooper)
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
                    closest.Player?.Name ?? "Unknown", MathF.Sqrt(closestDistSq));
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
    /// Automatically obtains position from player's pawn and finds nearest dead player as victim
    /// Must be called from the main game thread
    /// </summary>
    public SpawnPoopResult SpawnPoopWithFullLogic(
        IGameClient? player,
        float size,
        PoopColorPreference colorPreference,
        bool playSounds = true,
        bool showMessages = true)
    {
        // Validate and extract player information upfront
        if (player == null || !player.IsValid)
        {
            logger.LogWarning("SpawnPoopWithFullLogic called with invalid player");
            return new SpawnPoopResult();
        }

        // Get controller and pawn
        var playerController = playerManager.GetController(player);
        if (playerController == null || !playerController.IsValid())
        {
            logger.LogWarning("SpawnPoopWithFullLogic: Player has no valid controller");
            return new SpawnPoopResult();
        }

        var pawn = playerController.GetPlayerPawn();
        if (pawn == null || !pawn.IsValid())
        {
            logger.LogWarning("SpawnPoopWithFullLogic: Player has no valid pawn");
            return new SpawnPoopResult();
        }

        var position = pawn.GetAbsOrigin();

        string playerName = player.Name;
        SteamID playerSteamId = player.SteamId;
        
        // Find nearest dead player automatically
        var victimInfo = FindNearestDeadPlayer(player);
        var victim = victimInfo?.Player;
        
        // Extract victim information if present
        string? victimName = victim?.Name;
        SteamID? victimSteamId = victim?.SteamId;

        // Spawn the poop using the low-level spawner
        var result = SpawnPoop(position, size, colorPreference);

        if (result.Entity == null)
        {
            logger.LogError("Failed to spawn poop at position {position}", position);
            
            // Fire internal event for failed spawn
            PoopSpawnedInternal?.Invoke(new PoopSpawnedInternalEventArgs 
            { 
                Player = player,
                Position = position,
                Size = size,
                Victim = victim,
                Success = false 
            });
            
            return result;
        }

        float poopSize = result.Size ?? size;
        string sizeDesc = sizeGenerator.GetSizeDescription(poopSize);

        // Fire internal event for successful spawn
        PoopSpawnedInternal?.Invoke(new PoopSpawnedInternalEventArgs
        {
            Player = player,
            Position = position,
            Size = poopSize,
            Victim = victim,
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
            if (sizeGenerator.IsMassive(poopSize))
            {
                bridge.ModSharp.PrintToChatAll(
                    Format.ChatMessage(locale.GetString("poop.spawned_massive", new Dictionary<string, object>
                    {
                        ["playerName"] = playerName,
                        ["sizeDesc"] = sizeDesc,
                        ["size"] = poopSize
                    })));
            }
            else if (victim != null && victimName != null)
            {
                // Announce to all players in chat with size info
                bridge.ModSharp.PrintToChatAll(
                    Format.ChatMessage(locale.GetString("poop.spawned_on_player", new Dictionary<string, object>
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
                var controller = playerManager.GetController(player);
                controller?.PrintToChat(locale.GetString("poop.spawned_self", new Dictionary<string, object>
                {
                    ["sizeDesc"] = sizeDesc,
                    ["size"] = poopSize
                }));
            }
        }

        // Save to database (background thread)
        var currentMap = bridge.ModSharp.GetMapName() ?? "unknown";
        _ = Task.Run(async () =>
        {
            try
            {
                var logRecord = new PoopLogRecord
                {
                    PlayerName = playerName,
                    PlayerSteamId = playerSteamId.ToString(),
                    TargetName = victimName,
                    TargetSteamId = victimSteamId?.ToString(),
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
                if (victimSteamId != null && victimName != null)
                {
                    int victimCount = await database.GetVictimPoopCountAsync(victimSteamId.Value);

                    if (victimCount > 0)
                    {
                        await bridge.ModSharp.InvokeFrameActionAsync(() =>
                        {
                            bridge.ModSharp.PrintToChatAll(
                                Format.ChatMessage(locale.GetString("leaderboard.victim_total_count", new Dictionary<string, object>
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

    public bool Init()
    {
        return true;
    }
}
