namespace Prefix.Poop.Shared.Models;

/// <summary>
/// Statistics for a player's poop activity
/// </summary>
public sealed class PoopStats
{
    /// <summary>
    /// Player's name
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Player's SteamID64
    /// </summary>
    public string SteamId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of poops placed by this player
    /// </summary>
    public int PoopsPlaced { get; set; }

    /// <summary>
    /// Total number of times this player was pooped on
    /// </summary>
    public int TimesPoopedOn { get; set; }
}
