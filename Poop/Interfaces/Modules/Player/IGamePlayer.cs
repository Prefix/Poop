using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Prefix.Poop.Interfaces.Modules.Player;

/// <summary>
/// Wrapper for IGameClient that adds admin functionality and controller access
/// </summary>
public interface IGamePlayer
{
    /// <summary>
    /// The underlying game client
    /// </summary>
    IGameClient Client { get; }

    /// <summary>
    /// Whether this player is an admin
    /// </summary>
    bool IsAdmin { get; }

    /// <summary>
    /// Player's SteamID64
    /// </summary>
    SteamID SteamId { get; }

    /// <summary>
    /// Player's name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Player's slot index
    /// </summary>
    int Slot { get; }

    /// <summary>
    /// The player's controller entity (available after OnClientPutInServer)
    /// </summary>
    IPlayerController? Controller { get; }

    /// <summary>
    /// Whether this is a fake client (bot)
    /// </summary>
    bool IsFakeClient { get; }

    /// <summary>
    /// Check if this player is still valid (connected and not disconnected)
    /// </summary>
    bool IsValid();
}
