using System.Collections.Generic;
using Prefix.Poop.Interfaces;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Interfaces.Modules.Player;

internal interface IPlayerManager : IManager
{
    /// <summary>
    /// Get a wrapped IGamePlayer from an IGameClient
    /// Returns null if the player is not connected or doesn't match the client
    /// </summary>
    IGamePlayer? GetPlayer(IGameClient client);

    /// <summary>
    /// Get a wrapped IGamePlayer by slot
    /// Returns null if the slot is invalid or player is not valid
    /// </summary>
    IGamePlayer? GetPlayer(int slot);

    /// <summary>
    /// Get a wrapped IGamePlayer by SteamID
    /// Returns null if no player with that SteamID is found
    /// </summary>
    IGamePlayer? GetPlayerBySteamId(string steamId);

    /// <summary>
    /// Get all valid players
    /// </summary>
    /// <param name="ignoreFakeClient">Whether to exclude fake clients (bots)</param>
    IGamePlayer[] GetAllPlayers(bool ignoreFakeClient = true);

    /// <summary>
    /// Get a cached IPlayerController from an IGameClient
    /// Returns null if the controller hasn't been cached yet (player not fully connected)
    /// </summary>
    IPlayerController? GetController(IGameClient client);

    /// <summary>
    /// Get a cached IPlayerController by slot
    /// Returns null if the controller hasn't been cached yet
    /// </summary>
    IPlayerController? GetController(int slot);

    /// <summary>
    /// Check if a SteamID is an admin
    /// </summary>
    bool IsAdmin(string steamId);

    /// <summary>
    /// Get all players
    /// </summary>
    IEnumerable<IPlayerController> GetPlayers();

    /// <summary>
    /// Get all alive players
    /// </summary>
    IEnumerable<IPlayerController> GetAlive();

    /// <summary>
    /// Get all CT players
    /// </summary>
    IEnumerable<IPlayerController> GetCTs();

    /// <summary>
    /// Get all T players
    /// </summary>
    IEnumerable<IPlayerController> GetTs();

    /// <summary>
    /// Get all spectators
    /// </summary>
    IEnumerable<IPlayerController> GetSpecs();

    /// <summary>
    /// Check if any CT is alive
    /// </summary>
    bool IsCtAlive();

    /// <summary>
    /// Check if any T is alive
    /// </summary>
    bool IsTAlive();

    /// <summary>
    /// Check if any player on specified team is alive
    /// </summary>
    bool IsTeamAlive(CStrikeTeam team);
}
