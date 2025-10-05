using System.Threading.Tasks;
using Prefix.Poop.Modules.PoopModule;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Units;

namespace Prefix.Poop.Interfaces.PoopModule;

/// <summary>
/// Interface for poop database operations
/// </summary>
internal interface IPoopDatabase : IModule
{
    /// <summary>
    /// Saves a player's poop color preference
    /// </summary>
    Task SaveColorPreferenceAsync(SteamID steamId, PoopColorPreference preference);

    /// <summary>
    /// Loads a player's poop color preference
    /// </summary>
    Task<PoopColorPreference?> LoadColorPreferenceAsync(SteamID steamId);

    /// <summary>
    /// Logs an individual poop placement event with full details
    /// </summary>
    Task<int> LogPoopAsync(PoopLogRecord record);

    /// <summary>
    /// Gets recent poop logs with optional filters
    /// </summary>
    Task<PoopLogRecord[]> GetRecentPoopsAsync(int limit = 100, SteamID? playerSteamId = null, string? mapName = null);

    /// <summary>
    /// Gets the total number of poops logged
    /// </summary>
    Task<int> GetTotalPoopsCountAsync();

    /// <summary>
    /// Gets the number of times a player has been pooped on (victim count from poop_logs)
    /// </summary>
    Task<int> GetVictimPoopCountAsync(SteamID targetSteamId);

    /// <summary>
    /// Gets top poopers (players who placed the most poops) from poop_logs
    /// </summary>
    Task<TopPooperRecord[]> GetTopPoopersAsync(int limit = 10);

    /// <summary>
    /// Gets top victims (players who were pooped on the most) from poop_logs
    /// </summary>
    Task<TopVictimRecord[]> GetTopVictimsAsync(int limit = 10);
}
