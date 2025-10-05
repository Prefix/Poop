using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Prefix.Poop.Modules.PoopPlayer;

/// <summary>
/// Manages player-specific poop data (color preferences, etc.)
/// Acts as a bridge between PlayerManager and poop-specific features
/// </summary>
internal sealed class PoopPlayerManager(
    ILogger<PoopPlayerManager> logger,
    IPoopDatabase database,
    IClientListenerManager clientListenerManager,
    IPlayerManager playerManager)
    : IPoopPlayerManager
{
    // Cache of player color preferences (SteamID -> Preference)
    // Once loaded, preferences are cached for the entire session
    private readonly Dictionary<ulong, PoopColorPreference> _colorCache = new();
    private readonly object _cacheLock = new();

    // Track which SteamIDs are currently being loaded to prevent duplicate DB calls
    private readonly HashSet<ulong> _loadingPreferences = new();

    public bool Init()
    {
        clientListenerManager.ClientPutInServer += OnClientPutInServer;
        clientListenerManager.ClientDisconnected += OnClientDisconnected;
        logger.LogInformation("PoopPlayerManager initialized");
        return true;
    }

    public void Shutdown()
    {
        clientListenerManager.ClientPutInServer -= OnClientPutInServer;
        clientListenerManager.ClientDisconnected -= OnClientDisconnected;
        lock (_cacheLock)
        {
            _colorCache.Clear();
            _loadingPreferences.Clear();
        }
        logger.LogInformation("PoopPlayerManager shut down");
    }

    #region IPoopPlayerManager Implementation

    /// <summary>
    /// Gets a player's color preference (from cache or database)
    /// Always returns immediately with cached data or default if not yet loaded
    /// </summary>
    public async Task<PoopColorPreference> GetColorPreferenceAsync(SteamID steamId)
    {
        // Check cache first
        lock (_cacheLock)
        {
            if (_colorCache.TryGetValue(steamId, out var cached))
            {
                return cached;
            }

            // Check if already loading to prevent duplicate DB calls
            if (!_loadingPreferences.Add(steamId))
            {
                // Return default while loading in background
                return new PoopColorPreference(139, 69, 19);
            }

            // Mark as loading
        }

        // Load from database
        try
        {
            var pref = await database.LoadColorPreferenceAsync(steamId);
            if (pref != null)
            {
                lock (_cacheLock)
                {
                    _colorCache[steamId] = pref;
                    _loadingPreferences.Remove(steamId);
                }
                return pref;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load color preference for {steamId}, using default", steamId);
            lock (_cacheLock)
            {
                _loadingPreferences.Remove(steamId);
            }
        }

        // Return default brown color
        var defaultPref = new PoopColorPreference(139, 69, 19);
        
        // Cache the default so we don't keep hitting the DB for players without preferences
        lock (_cacheLock)
        {
            _colorCache[steamId] = defaultPref;
            _loadingPreferences.Remove(steamId);
        }
        
        return defaultPref;
    }

    /// <summary>
    /// Gets a player's color preference synchronously from cache only
    /// Returns default if not cached
    /// </summary>
    public PoopColorPreference GetColorPreference(SteamID steamId)
    {
        lock (_cacheLock)
        {
            if (_colorCache.TryGetValue(steamId, out var cached))
            {
                return cached;
            }
        }

        // Return default brown color
        return new PoopColorPreference(139, 69, 19);
    }

    /// <summary>
    /// Saves a player's color preference to database and cache
    /// </summary>
    public async Task SaveColorPreferenceAsync(SteamID steamId, PoopColorPreference preference)
    {
        try
        {
            // Save to database
            await database.SaveColorPreferenceAsync(steamId, preference);

            // Update cache
            lock (_cacheLock)
            {
                _colorCache[steamId] = preference;
            }

            logger.LogDebug("Saved color preference for {steamId}: RGB({r},{g},{b}) Rainbow={rainbow} Random={random}",
                steamId, preference.Red, preference.Green, preference.Blue,
                preference.IsRainbow, preference.IsRandom);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save color preference for {steamId}", steamId);
            throw;
        }
    }

    /// <summary>
    /// Clears cached color preference for a player (e.g., on disconnect)
    /// Note: Normally called automatically on disconnect via event handler
    /// </summary>
    public void ClearColorCache(SteamID steamId)
    {
        lock (_cacheLock)
        {
            _colorCache.Remove(steamId);
            _loadingPreferences.Remove(steamId);
        }
    }

    /// <summary>
    /// Gets a player by SteamID
    /// </summary>
    public IGamePlayer? GetPlayerBySteamId(SteamID steamId)
    {
        return playerManager.GetPlayerBySteamId(steamId);
    }

    /// <summary>
    /// Gets a player by slot
    /// </summary>
    public IGamePlayer? GetPlayer(int slot)
    {
        return playerManager.GetPlayer(slot);
    }

    #endregion

    #region Client Event Handlers

    /// <summary>
    /// Called when a player is put in server
    /// Preload color preference from database into cache
    /// </summary>
    private void OnClientPutInServer(IGameClient client)
    {
        if (!client.IsValid || client.IsHltv || client.IsFakeClient)
        {
            return;
        }

        if (!ulong.TryParse(client.SteamId.ToString(), out var steamId))
        {
            logger.LogWarning("Invalid SteamID for client: {steamId}", client.SteamId);
            return;
        }

        // Preload color preference asynchronously
        _ = PreloadColorPreferenceAsync(client.Name, steamId);
    }

    /// <summary>
    /// Async method to preload player color preference into cache
    /// </summary>
    private async Task PreloadColorPreferenceAsync(string playerName, SteamID steamId)
    {
        // Check if already in cache or loading
        lock (_cacheLock)
        {
            if (_colorCache.ContainsKey(steamId) || !_loadingPreferences.Add(steamId))
            {
                return;
            }
        }

        try
        {
            var preference = await database.LoadColorPreferenceAsync(steamId);

            lock (_cacheLock)
            {
                _loadingPreferences.Remove(steamId);
                
                if (preference != null)
                {
                    _colorCache[steamId] = preference;
                    logger.LogDebug("Preloaded color preference for {name}: RGB({r},{g},{b}) Rainbow={rainbow} Random={random}",
                        playerName, preference.Red, preference.Green, preference.Blue,
                        preference.IsRainbow, preference.IsRandom);
                }
                else
                {
                    // Cache default to avoid future DB lookups
                    _colorCache[steamId] = new PoopColorPreference(139, 69, 19);
                    logger.LogDebug("No color preference found for {name}, cached default", playerName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to preload color preference for {name}", playerName);
            lock (_cacheLock)
            {
                _loadingPreferences.Remove(steamId);
                // Cache default on error to avoid repeated failed DB calls
                _colorCache[steamId] = new PoopColorPreference(139, 69, 19);
            }
        }
    }

    /// <summary>
    /// Called when a player disconnects
    /// Clear cached data for this player
    /// </summary>
    private void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (!ulong.TryParse(client.SteamId.ToString(), out var steamId))
        {
            return;
        }

        lock (_cacheLock)
        {
            _colorCache.Remove(steamId);
            _loadingPreferences.Remove(steamId);
        }

        logger.LogDebug("Cleared color cache for {name} (SteamID: {steamId}, Reason: {reason})",
            client.Name, steamId, reason);
    }

    #endregion
}
