using System;
using System.Linq;
using System.Threading.Tasks;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Database;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Interfaces.Modules.Player;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Shared;
using Prefix.Poop.Shared.Events;
using Prefix.Poop.Shared.Models;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Types;

namespace Prefix.Poop.Modules.SharedInterface;

/// <summary>
/// Implements the public API for the Poop plugin
/// Allows other plugins to spawn poops, get stats, and subscribe to events
/// </summary>
internal sealed class SharedInterface(
    ILogger<SharedInterface> logger,
    InterfaceBridge bridge,
    IPoopDatabase database,
    IPoopSpawner spawner,
    IPlayerManager playerManager,
    IPoopPlayerManager poopPlayerManager,
    IConfigManager config,
    IPoopColorMenu colorMenu)
    : IModule, IPoopShared
{
    public bool Init()
        => true;

    public void OnPostInit()
    {
        // Register API methods as dynamic natives for cross-plugin calls
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.SpawnPoop", SpawnPoopNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.ForcePlayerPoop", ForcePlayerPoopNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetPlayerStatsAsync", GetPlayerStatsAsyncNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetTopPoopersAsync", GetTopPoopersAsyncNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetTopVictimsAsync", GetTopVictimsAsyncNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetTotalPoopsCountAsync", GetTotalPoopsCountAsyncNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetPlayerColorPreferenceAsync", GetPlayerColorPreferenceAsyncNative);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.SetPlayerColorPreferenceAsync", SetPlayerColorPreferenceAsyncNative);

        // Register the shared interface itself for direct C# plugin access
        bridge.SharpModuleManager.RegisterSharpModuleInterface(bridge.Poop, IPoopShared.Identity, this);

        // Subscribe to internal poop spawn events from PoopSpawner
        spawner.PoopSpawnedInternal += OnPoopSpawnedInternal;

        logger.LogInformation("Poop API registered successfully with {methodCount} methods", 8);
    }

    #region Events

    public event Action<PoopCommandEventArgs>? OnPoopCommand;
    public event Action<PoopSpawnedEventArgs>? OnPoopSpawned;

    /// <summary>
    /// Internal method to fire the OnPoopCommand event
    /// Returns true if command should proceed, false if cancelled
    /// </summary>
    internal bool FirePoopCommand(IGamePlayer player, string commandName)
    {
        if (OnPoopCommand == null) return true;

        var args = new PoopCommandEventArgs
        {
            PlayerSteamId = player.SteamId,
            PlayerName = player.Name,
            CommandName = commandName,
            Cancel = false
        };

        OnPoopCommand?.Invoke(args);
        
        return !args.Cancel;
    }

    /// <summary>
    /// Internal method to fire the OnPoopSpawned event
    /// </summary>
    internal void FirePoopSpawned(IGamePlayer? player, Vector position, float size, string? victimName, string? victimSteamId, bool isCommandTriggered, bool success)
    {
        if (OnPoopSpawned == null) return;

        var args = new PoopSpawnedEventArgs
        {
            PlayerSteamId = player?.SteamId ?? string.Empty,
            PlayerName = player?.Name ?? "Unknown",
            Position = position,
            Size = size,
            VictimName = victimName,
            VictimSteamId = victimSteamId,
            IsCommandTriggered = isCommandTriggered,
            Success = success
        };

        OnPoopSpawned?.Invoke(args);
    }

    /// <summary>
    /// Handler for internal poop spawn events from PoopSpawner
    /// Translates to public API events
    /// </summary>
    private void OnPoopSpawnedInternal(PoopSpawnedInternalEventArgs args)
    {
        // Get player from SteamID
        var player = playerManager.GetPlayerBySteamId(args.PlayerSteamId);
        
        // Determine if it was command-triggered based on whether we have a real player
        // (API calls use "API" as steam ID)
        bool isCommandTriggered = args.PlayerSteamId != "API" && player != null;

        FirePoopSpawned(
            player: player,
            position: args.Position,
            size: args.Size,
            victimName: args.VictimName,
            victimSteamId: args.VictimSteamId,
            isCommandTriggered: isCommandTriggered,
            success: args.Success);
    }

    #endregion

    #region Spawn API

    public SpawnPoopResult SpawnPoop(
        Vector position,
        float size = -1f,
        PoopColorPreference? colorPreference = null,
        string? victimName = null,
        bool playSounds = true)
    {
        // Use default color if not provided
        PoopColorPreference effectiveColor;
        if (colorPreference != null)
        {
            effectiveColor = colorPreference;
        }
        else
        {
            var (r, g, b) = config.GetDefaultColorRgb();
            effectiveColor = new PoopColorPreference(r, g, b);
        }
        
        // Handle random mode
        if (effectiveColor.IsRandom)
        {
            effectiveColor = colorMenu.GetRandomColor();
        }

        // Use the spawn service with full logic
        var result = spawner.SpawnPoopWithFullLogic(
            playerSteamId: "API",
            position: position,
            size: size,
            colorPreference: effectiveColor,
            victimName: victimName,
            victimSteamId: null,
            playSounds: playSounds,
            showMessages: false); // Don't show messages for direct API calls

        // Event will fire automatically via PoopSpawner.PoopSpawnedInternal

        return result;
    }

    public SpawnPoopResult? ForcePlayerPoop(
        string playerSteamId,
        float size = -1f,
        PoopColorPreference? colorPreference = null,
        bool playSounds = true)
    {
        var player = playerManager.GetPlayerBySteamId(playerSteamId);
        if (player == null || !player.IsValid())
        {
            logger.LogWarning("ForcePlayerPoop: Player not found or invalid: {steamId}", playerSteamId);
            return null;
        }

        var controller = player.Controller;
        if (controller == null)
        {
            logger.LogWarning("ForcePlayerPoop: Controller not found for player {name}", player.Name);
            return null;
        }

        var pawn = controller.GetPlayerPawn();
        if (pawn == null || !pawn.IsValid())
        {
            logger.LogWarning("ForcePlayerPoop: Pawn not found for player {name}", player.Name);
            return null;
        }

        var position = pawn.GetAbsOrigin();

        // Find nearest dead player
        var victimInfo = spawner.FindNearestDeadPlayer(position, player.Slot);
        var spawnPos = victimInfo?.Position ?? position;
        var victimName = victimInfo?.PlayerName;
        var victimSteamId = victimInfo?.SteamId;

        // Get color preference: parameter > player's saved preference > default
        PoopColorPreference effectiveColor;
        if (colorPreference != null)
        {
            effectiveColor = colorPreference;
        }
        else if (config.EnableColorPreferences && ulong.TryParse(player.SteamId, out var steamId))
        {
            // Get from player manager (async call, but we'll use Task.Run to get it)
            var colorTask = poopPlayerManager.GetColorPreferenceAsync(steamId);
            effectiveColor = colorTask.Result; // Block on purpose for API simplicity
        }
        else
        {
            var (r, g, b) = config.GetDefaultColorRgb();
            effectiveColor = new PoopColorPreference(r, g, b);
        }

        // Handle random mode
        if (effectiveColor.IsRandom)
        {
            effectiveColor = colorMenu.GetRandomColor();
        }

        // Use the spawn service with full logic
        var result = spawner.SpawnPoopWithFullLogic(
            playerSteamId: playerSteamId,
            position: spawnPos,
            size: size,
            colorPreference: effectiveColor,
            victimName: victimName,
            victimSteamId: victimSteamId,
            playSounds: playSounds,
            showMessages: true); // Show messages for player-triggered poops

        // Event will fire automatically via PoopSpawner.PoopSpawnedInternal

        return result;
    }

    // Native wrapper methods for dynamic native calls
    private SpawnPoopResult SpawnPoopNative(float x, float y, float z, float size, int red, int green, int blue, bool isRainbow, bool playSounds)
    {
        var position = new Vector(x, y, z);
        PoopColorPreference? colorPreference = null;

        if (red >= 0 && green >= 0 && blue >= 0)
        {
            colorPreference = new PoopColorPreference(red, green, blue, isRainbow);
        }

        return SpawnPoop(position, size, colorPreference, null, playSounds);
    }

    private SpawnPoopResult? ForcePlayerPoopNative(string steamId, float size, int red, int green, int blue, bool isRainbow, bool playSounds)
    {
        PoopColorPreference? colorPreference = null;

        if (red >= 0 && green >= 0 && blue >= 0)
        {
            colorPreference = new PoopColorPreference(red, green, blue, isRainbow);
        }

        return ForcePlayerPoop(steamId, size, colorPreference, playSounds);
    }

    #endregion

    #region Statistics API

    public async Task<PoopStats?> GetPlayerStatsAsync(string steamId)
    {
        try
        {
            if (!ulong.TryParse(steamId, out var steamIdUlong))
            {
                logger.LogWarning("Invalid SteamID format: {steamId}", steamId);
                return null;
            }

            var database1 = database;
            
            // Get poop count from logs
            var logs = await database1.GetRecentPoopsAsync(limit: int.MaxValue, playerSteamId: steamIdUlong);
            var poopCount = logs.Length;

            // Get victim count
            var victimCount = await database1.GetVictimPoopCountAsync(steamId);

            // Get player name from current connected players or from logs
            var player = playerManager.GetPlayerBySteamId(steamId);
            var playerName = player?.Name ?? logs.FirstOrDefault()?.PlayerName ?? "Unknown";

            return new PoopStats
            {
                SteamId = steamId,
                PlayerName = playerName,
                PoopsPlaced = poopCount,
                TimesPoopedOn = victimCount
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting player stats for {steamId}", steamId);
            return null;
        }
    }

    public async Task<PoopStats[]> GetTopPoopersAsync(int limit = 10)
    {
        try
        {
            var topPoopers = await database.GetTopPoopersAsync(limit);
            return topPoopers.Select(p => new PoopStats
            {
                SteamId = p.SteamId,
                PlayerName = p.Name,
                PoopsPlaced = p.PoopCount,
                TimesPoopedOn = 0
            }).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting top poopers");
            return Array.Empty<PoopStats>();
        }
    }

    public async Task<PoopStats[]> GetTopVictimsAsync(int limit = 10)
    {
        try
        {
            var topVictims = await database.GetTopVictimsAsync(limit);
            return topVictims.Select(v => new PoopStats
            {
                SteamId = v.SteamId,
                PlayerName = v.Name,
                PoopsPlaced = 0,
                TimesPoopedOn = v.VictimCount
            }).ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting top victims");
            return Array.Empty<PoopStats>();
        }
    }

    public async Task<int> GetTotalPoopsCountAsync()
    {
        try
        {
            return await database.GetTotalPoopsCountAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting total poops count");
            return 0;
        }
    }

    // Native wrapper methods for async operations
    private Task<PoopStats?> GetPlayerStatsAsyncNative(string steamId) => GetPlayerStatsAsync(steamId);
    private Task<PoopStats[]> GetTopPoopersAsyncNative(int limit) => GetTopPoopersAsync(limit);
    private Task<PoopStats[]> GetTopVictimsAsyncNative(int limit) => GetTopVictimsAsync(limit);
    private Task<int> GetTotalPoopsCountAsyncNative() => GetTotalPoopsCountAsync();

    #endregion

    #region Player Color Preferences

    public async Task<PoopColorPreference?> GetPlayerColorPreferenceAsync(string steamId)
    {
        try
        {
            if (!ulong.TryParse(steamId, out var steamIdUlong))
            {
                logger.LogWarning("Invalid SteamID format: {steamId}", steamId);
                return null;
            }

            // Get from PoopPlayerManager (handles cache + database)
            return await poopPlayerManager.GetColorPreferenceAsync(steamIdUlong);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting color preference for {steamId}", steamId);
            return null;
        }
    }

    public async Task SetPlayerColorPreferenceAsync(string steamId, PoopColorPreference color)
    {
        try
        {
            if (!ulong.TryParse(steamId, out var steamIdUlong))
            {
                logger.LogWarning("Invalid SteamID format: {steamId}", steamId);
                return;
            }

            // Save via PoopPlayerManager (handles database + cache)
            await poopPlayerManager.SaveColorPreferenceAsync(steamIdUlong, color);

            logger.LogDebug("Set color preference for {steamId}: RGB({r},{g},{b}) Rainbow={rainbow}",
                steamId, color.Red, color.Green, color.Blue, color.IsRainbow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting color preference for {steamId}", steamId);
        }
    }

    // Native wrapper methods for color preference operations
    private Task<PoopColorPreference?> GetPlayerColorPreferenceAsyncNative(string steamId) => GetPlayerColorPreferenceAsync(steamId);
    private Task SetPlayerColorPreferenceAsyncNative(string steamId, int red, int green, int blue, bool isRainbow)
    {
        var colorPreference = new PoopColorPreference(red, green, blue, isRainbow);
        return SetPlayerColorPreferenceAsync(steamId, colorPreference);
    }

    #endregion

    public void Shutdown()
    {
        logger.LogInformation("Poop API shutting down");
    }
}
