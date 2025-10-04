using System.Threading.Tasks;
using Prefix.Poop.Interfaces.Modules.Player;
using Prefix.Poop.Shared.Models;

namespace Prefix.Poop.Interfaces.Modules;

/// <summary>
/// Interface for handling poop color selection menu and player color preferences
/// </summary>
internal interface IPoopColorMenu : IModule
{
    /// <summary>
    /// Opens the color selection menu for a player
    /// </summary>
    /// <param name="player">The player to show the menu to</param>
    Task OpenColorMenuAsync(IGamePlayer player);

    /// <summary>
    /// Gets a player's color preference (delegates to PoopPlayerManager)
    /// </summary>
    /// <param name="steamId">Player's Steam ID</param>
    /// <returns>The player's color preference or default brown</returns>
    Task<PoopColorPreference> GetPlayerColorPreferenceAsync(ulong steamId);

    /// <summary>
    /// Gets a random color from the available colors (excluding Rainbow and Random)
    /// </summary>
    /// <returns>A random color preference</returns>
    PoopColorPreference GetRandomColor();
}
