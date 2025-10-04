using System.Threading.Tasks;
using Prefix.Poop.Interfaces.Modules.Player;
using Prefix.Poop.Shared.Models;

namespace Prefix.Poop.Interfaces.Modules;

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
    Task<PoopColorPreference> GetColorPreferenceAsync(ulong steamId);

    /// <summary>
    /// Saves a player's color preference to database and cache
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <param name="preference">The color preference to save</param>
    Task SaveColorPreferenceAsync(ulong steamId, PoopColorPreference preference);

    /// <summary>
    /// Clears cached color preference when player disconnects
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    void ClearColorCache(ulong steamId);

    /// <summary>
    /// Gets a player by SteamID
    /// </summary>
    IGamePlayer? GetPlayerBySteamId(string steamId);

    /// <summary>
    /// Gets a player by slot
    /// </summary>
    IGamePlayer? GetPlayer(int slot);
}
