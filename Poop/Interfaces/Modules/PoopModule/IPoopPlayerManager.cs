using System.Threading.Tasks;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Units;

namespace Prefix.Poop.Interfaces.PoopModule;

/// <summary>
/// Manages player-specific poop data (extends PlayerManager with poop features)
/// </summary>
internal interface IPoopPlayerManager : IModule
{
    /// <summary>
    /// Gets a player's color preference (from cache or database)
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <returns>The player's color preference or default brown</returns>
    Task<PoopColorPreference> GetColorPreferenceAsync(SteamID steamId);

    /// <summary>
    /// Gets a player's color preference synchronously from cache only
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <returns>The cached color preference or default brown if not cached</returns>
    PoopColorPreference GetColorPreference(SteamID steamId);

    /// <summary>
    /// Saves a player's color preference to database and cache
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <param name="preference">The color preference to save</param>
    Task SaveColorPreferenceAsync(SteamID steamId, PoopColorPreference preference);

    /// <summary>
    /// Clears cached color preference when player disconnects
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    void ClearColorCache(SteamID steamId);

    /// <summary>
    /// Gets a player by SteamID
    /// </summary>
    IGamePlayer? GetPlayerBySteamId(SteamID steamId);

    /// <summary>
    /// Gets a player by slot
    /// </summary>
    IGamePlayer? GetPlayer(int slot);
}
