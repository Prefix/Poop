// ReSharper disable InconsistentNaming, UnusedMember.Global, UnusedMemberInSuper.Global

using System.Threading.Tasks;
using Prefix.Poop.Shared.Events;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Types;

namespace Prefix.Poop.Shared;

/// <summary>
/// Public API for the Poop plugin
/// Allows other plugins to interact with poop functionality
/// </summary>
public interface IPoopShared
{
    static string Identity => typeof(IPoopShared).FullName ?? nameof(IPoopShared);

    #region Events

    /// <summary>
    /// Fired when a player executes a poop command (before any validation)
    /// Use this to block commands based on permissions (e.g., donator-only, admin-only, etc.)
    /// This fires BEFORE cooldown checks, alive checks, and any other validation
    /// </summary>
    event System.Action<PoopCommandEventArgs>? OnPoopCommand;

    /// <summary>
    /// Fired after a poop has been successfully spawned
    /// Use this for logging, stats tracking, custom effects, etc.
    /// </summary>
    event System.Action<PoopSpawnedEventArgs>? OnPoopSpawned;

    #endregion

    #region Spawn API

    /// <summary>
    /// Spawn a poop at a specific position without restrictions
    /// </summary>
    /// <param name="position">World position to spawn the poop</param>
    /// <param name="size">Size of the poop (-1 for random, or specify between MinPoopSize and MaxPoopSize)</param>
    /// <param name="color">Color preference for the poop (null for default brown)</param>
    /// <param name="victimName">Optional victim name for logging</param>
    /// <param name="playSounds">Whether to play poop sounds</param>
    /// <returns>Result containing the spawned entity and details</returns>
    SpawnPoopResult SpawnPoop(
        Vector position,
        float size = -1.0f,
        PoopColorPreference? color = null,
        string? victimName = null,
        bool playSounds = true);

    /// <summary>
    /// Spawn a poop on a player's position (forces spawn, bypassing cooldowns and restrictions)
    /// </summary>
    /// <param name="playerSteamId">SteamID64 of the player to spawn poop from</param>
    /// <param name="size">Size of the poop (-1 for random)</param>
    /// <param name="color">Color preference for the poop (null to use player's preference or default)</param>
    /// <param name="playSounds">Whether to play poop sounds</param>
    /// <returns>Result containing the spawned entity and details</returns>
    SpawnPoopResult? ForcePlayerPoop(
        string playerSteamId,
        float size = -1.0f,
        PoopColorPreference? color = null,
        bool playSounds = true);

    #endregion

    #region Statistics API

    /// <summary>
    /// Get statistics for a specific player
    /// </summary>
    /// <param name="steamId">Player's SteamID64</param>
    /// <returns>Player's poop statistics (null if not found)</returns>
    Task<PoopStats?> GetPlayerStatsAsync(string steamId);

    /// <summary>
    /// Get the top N players who placed the most poops
    /// </summary>
    /// <param name="limit">Number of top players to return</param>
    /// <returns>Array of player stats sorted by poops placed</returns>
    Task<PoopStats[]> GetTopPoopersAsync(int limit = 10);

    /// <summary>
    /// Get the top N players who were pooped on the most
    /// </summary>
    /// <param name="limit">Number of top players to return</param>
    /// <returns>Array of player stats sorted by times pooped on</returns>
    Task<PoopStats[]> GetTopVictimsAsync(int limit = 10);

    /// <summary>
    /// Get the total number of poops spawned on the server (all time)
    /// </summary>
    /// <returns>Total poop count</returns>
    Task<int> GetTotalPoopsCountAsync();

    #endregion

    #region Player Color Preferences

    /// <summary>
    /// Get a player's preferred poop color
    /// </summary>
    /// <param name="steamId">Player's SteamID64</param>
    /// <returns>Player's color preference (null if not set)</returns>
    Task<PoopColorPreference?> GetPlayerColorPreferenceAsync(string steamId);

    /// <summary>
    /// Set a player's poop color preference
    /// </summary>
    /// <param name="steamId">Player's SteamID64</param>
    /// <param name="color">New color preference</param>
    Task SetPlayerColorPreferenceAsync(string steamId, PoopColorPreference color);

    #endregion
}
