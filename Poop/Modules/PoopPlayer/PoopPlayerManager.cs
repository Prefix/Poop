using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Database;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Interfaces.Modules.Player;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopPlayer;

/// <summary>
/// Manages player-specific poop data (color preferences, etc.)
/// Acts as a bridge between PlayerManager and poop-specific features
/// </summary>
internal sealed class PoopPlayerManager : IPoopPlayerManager, IClientListener
{
    private readonly ILogger<PoopPlayerManager> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly IPoopDatabase _database;
    private readonly IPlayerManager _playerManager;

    // Cache of player color preferences (SteamID -> Preference)
    private readonly Dictionary<ulong, PoopColorPreference> _colorCache = new();
    private readonly object _cacheLock = new();

    public PoopPlayerManager(
        ILogger<PoopPlayerManager> logger,
        InterfaceBridge bridge,
        IPoopDatabase database,
        IPlayerManager playerManager)
    {
        _logger = logger;
        _bridge = bridge;
        _database = database;
        _playerManager = playerManager;
    }

    public bool Init()
    {
        _bridge.ClientManager.InstallClientListener(this);
        _logger.LogInformation("PoopPlayerManager initialized");
        return true;
    }

    public void OnPostInit()
    {
    }

    public void OnAllSharpModulesLoaded()
    {
    }

    public void Shutdown()
    {
        _bridge.ClientManager.RemoveClientListener(this);
        lock (_cacheLock)
        {
            _colorCache.Clear();
        }
        _logger.LogInformation("PoopPlayerManager shut down");
    }

    #region IPoopPlayerManager Implementation

    /// <summary>
    /// Gets a player's color preference (from cache or database)
    /// </summary>
    public async Task<PoopColorPreference> GetColorPreferenceAsync(ulong steamId)
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (_colorCache.TryGetValue(steamId, out var cached))
            {
                return cached;
            }
        }

        // Load from database
        try
        {
            var pref = await _database.LoadColorPreferenceAsync(steamId);
            if (pref != null)
            {
                lock (_cacheLock)
                {
                    _colorCache[steamId] = pref;
                }
                return pref;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load color preference for {steamId}, using default", steamId);
        }

        // Return default brown color
        var defaultPref = new PoopColorPreference(139, 69, 19);
        return defaultPref;
    }

    /// <summary>
    /// Saves a player's color preference to database and cache
    /// </summary>
    public async Task SaveColorPreferenceAsync(ulong steamId, PoopColorPreference preference)
    {
        try
        {
            // Save to database
            await _database.SaveColorPreferenceAsync(steamId, preference);

            // Update cache
            lock (_cacheLock)
            {
                _colorCache[steamId] = preference;
            }

            _logger.LogDebug("Saved color preference for {steamId}: RGB({r},{g},{b}) Rainbow={rainbow} Random={random}",
                steamId, preference.Red, preference.Green, preference.Blue,
                preference.IsRainbow, preference.IsRandom);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save color preference for {steamId}", steamId);
            throw;
        }
    }

    /// <summary>
    /// Clears cached color preference for a player (e.g., on disconnect)
    /// </summary>
    public void ClearColorCache(ulong steamId)
    {
        lock (_cacheLock)
        {
            _colorCache.Remove(steamId);
        }
    }

    /// <summary>
    /// Gets a player by SteamID
    /// </summary>
    public IGamePlayer? GetPlayerBySteamId(string steamId)
    {
        return _playerManager.GetPlayerBySteamId(steamId);
    }

    /// <summary>
    /// Gets a player by slot
    /// </summary>
    public IGamePlayer? GetPlayer(int slot)
    {
        return _playerManager.GetPlayer(slot);
    }

    #endregion

    #region IClientListener Implementation

    public void OnClientConnected(IGameClient client)
    {
        // Nothing to do here
    }

    public void OnClientPutInServer(IGameClient client)
    {
        // Nothing to do here
    }

    /// <summary>
    /// Called after admin check (player is authorized)
    /// Preload color preference from database into cache
    /// </summary>
    public void OnClientPostAdminCheck(IGameClient client)
    {
        if (!client.IsValid || client.IsHltv || client.IsFakeClient)
        {
            return;
        }

        if (!ulong.TryParse(client.SteamId.ToString(), out var steamId))
        {
            _logger.LogWarning("Invalid SteamID for client: {steamId}", client.SteamId);
            return;
        }

        // Preload color preference asynchronously
        _ = PreloadColorPreferenceAsync(client.Name, steamId);
    }

    /// <summary>
    /// Async method to preload player color preference into cache
    /// </summary>
    private async Task PreloadColorPreferenceAsync(string playerName, ulong steamId)
    {
        try
        {
            var preference = await _database.LoadColorPreferenceAsync(steamId);

            if (preference != null)
            {
                lock (_cacheLock)
                {
                    _colorCache[steamId] = preference;
                }

                _logger.LogDebug("Preloaded color preference for {name}: RGB({r},{g},{b}) Rainbow={rainbow} Random={random}",
                    playerName, preference.Red, preference.Green, preference.Blue,
                    preference.IsRainbow, preference.IsRandom);
            }
            else
            {
                _logger.LogDebug("No color preference found for {name}, will use default", playerName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preload color preference for {name}", playerName);
        }
    }

    /// <summary>
    /// Called when a player disconnects
    /// Clear cached data for this player
    /// </summary>
    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (!ulong.TryParse(client.SteamId.ToString(), out var steamId))
        {
            return;
        }

        ClearColorCache(steamId);

        _logger.LogDebug("Cleared color cache for {name} (SteamID: {steamId})", client.Name, steamId);
    }

    public int ListenerVersion => IClientListener.ApiVersion;
    public int ListenerPriority => 1; // Run after PlayerManager (priority 0)

    #endregion
}
