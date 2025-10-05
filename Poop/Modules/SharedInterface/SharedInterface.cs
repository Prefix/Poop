using System;
using System.Linq;
using System.Threading.Tasks;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Shared;
using Prefix.Poop.Shared.Events;
using Prefix.Poop.Shared.Models;
using Prefix.Poop.Utils;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers.Player;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;
using Sharp.Shared.Units;

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
    IConfigManager config)
    : IModule, IPoopShared
{
    public bool Init()
        => true;

    public void OnPostInit()
    {
        // Register API methods as dynamic natives for cross-plugin calls
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.SpawnPoop", SpawnPoop);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.ForcePlayerPoop", ForcePlayerPoop);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetPlayerStatsAsync", GetPlayerStatsAsync);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetTopPoopersAsync", GetTopPoopersAsync);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetTopVictimsAsync", GetTopVictimsAsync);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetTotalPoopsCountAsync", GetTotalPoopsCountAsync);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.GetPlayerColorPreferenceAsync", GetPlayerColorPreferenceAsync);
        bridge.SharpModuleManager.RegisterDynamicNative(bridge.Poop, $"{IPoopShared.Identity}.SetPlayerColorPreferenceAsync", SetPlayerColorPreferenceAsync);

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
            Player = player.Client,
            CommandName = commandName,
            Cancel = false
        };

        OnPoopCommand?.Invoke(args);
        
        return !args.Cancel;
    }

    /// <summary>
    /// Internal method to fire the OnPoopSpawned event
    /// </summary>
    internal void FirePoopSpawned(IGameClient? player, Vector position, float size, IGameClient? victim, bool isCommandTriggered, bool success)
    {
        if (OnPoopSpawned == null) return;

        var args = new PoopSpawnedEventArgs
        {
            Player = player!,
            Position = position,
            Size = size,
            Victim = victim!,
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
        // Determine if it was command-triggered based on whether we have a real player
        // (API calls use "API" as steam ID)
        bool isCommandTriggered = args.Player != null;

        FirePoopSpawned(
            player: args.Player,
            position: args.Position,
            size: args.Size,
            victim: args.Victim,
            isCommandTriggered: isCommandTriggered,
            success: args.Success);
    }

    #endregion

    #region Spawn API
    public SpawnPoopResult SpawnPoop(
        IGameClient? player,
        Vector position,
        float size = -1f,
        PoopColorPreference? colorPreference = null,
        IGameClient? victim = null,
        bool playSounds = true)
    {
        // Note: position parameter is ignored - we get it from player's pawn automatically
        // victim parameter is also ignored - we find nearest dead player automatically
        
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
            effectiveColor = ColorUtils.GetRandomColor(config.AvailableColors);
        }

        // Use the full logic method - it handles position and victim internally
        var result = spawner.SpawnPoopWithFullLogic(
            player: player,
            size: size,
            colorPreference: effectiveColor,
            playSounds: playSounds,
            showMessages: false); // Don't show messages for direct API calls

        return result;
    }

    public SpawnPoopResult? ForcePlayerPoop(
        IGameClient? client,
        float size = -1f,
        PoopColorPreference? color = null,
        bool playSounds = true)
    {
        if (client == null)
        {
            return null;
        }

        // Get the player from the client
        var player = playerManager.GetPlayerBySteamId(client.SteamId);
        if (player == null || !player.IsValid())
        {
            return null;
        }


        color ??= poopPlayerManager.GetColorPreference(player.SteamId);

        // Use the spawn service with full logic
        // It will automatically get position from player's pawn and find nearest victim
        var result = spawner.SpawnPoopWithFullLogic(
            player: client,
            size: size,
            colorPreference: color,
            playSounds: playSounds,
            showMessages: true); // Show messages for player-triggered poops

        // Event will fire automatically via PoopSpawner.PoopSpawnedInternal

        return result;
    }

    #endregion

    #region Statistics API

    public async Task<PoopStats?> GetPlayerStatsAsync(SteamID steamId)
    {
        try
        {            
            // Get poop count from logs
            var logs = await database.GetRecentPoopsAsync(limit: int.MaxValue, playerSteamId: steamId);
            var poopCount = logs.Length;

            // Get victim count
            var victimCount = await database.GetVictimPoopCountAsync(steamId);

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

    #endregion

    #region Player Color Preferences

    public async Task<PoopColorPreference?> GetPlayerColorPreferenceAsync(SteamID steamId)
    {
        try
        {
            // Get via PoopPlayerManager (checks cache first, then database)
            return await poopPlayerManager.GetColorPreferenceAsync(steamId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting color preference for {steamId}", steamId);
            return null;
        }
    }

    public async Task SetPlayerColorPreferenceAsync(SteamID steamId, PoopColorPreference color)
    {
        try
        {
            // Save via PoopPlayerManager (handles database + cache)
            await poopPlayerManager.SaveColorPreferenceAsync(steamId, color);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting color preference for {steamId}", steamId);
        }
    }

    #endregion

    public void Shutdown()
    {
        logger.LogInformation("Poop API shutting down");
    }
}
