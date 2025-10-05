using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Prefix.Poop.Managers.Player;

internal class PlayerManager(
    InterfaceBridge bridge,
    ILogger<PlayerManager> logger,
    IClientListenerManager clientListenerManager,
    ConfigManager config)
    : IPlayerManager
{
    private readonly GamePlayer?[] _players = new GamePlayer?[PlayerSlot.MaxPlayerSlot];

    public bool Init()
    {
        clientListenerManager.ClientPutInServer += OnClientPutInServer;
        clientListenerManager.ClientDisconnected += OnClientDisconnected;
        logger.LogInformation("PlayerManager initialized with {count} admins", config.AdminSteamIds.Count);
        return true;
    }

    public void Shutdown()
    {
        clientListenerManager.ClientPutInServer -= OnClientPutInServer;
        clientListenerManager.ClientDisconnected -= OnClientDisconnected;

        // Clear all player references
        for (int i = 0; i < _players.Length; i++)
        {
            _players[i] = null;
        }
    }

    public IGamePlayer? GetPlayer(IGameClient client)
    {
        if (client.Slot < 0 || client.Slot >= _players.Length)
        {
            return null;
        }

        var player = _players[client.Slot];

        // Validate that the player matches the client
        if (player is null || !player.IsValid() || !player.Client.Equals(client))
        {
            return null;
        }

        return player;
    }

    public IGamePlayer? GetPlayer(int slot)
    {
        if (slot < 0 || slot >= _players.Length)
        {
            return null;
        }

        var player = _players[slot];
        return player is not null && player.IsValid() ? player : null;
    }

    public IGamePlayer? GetPlayerBySteamId(SteamID steamId)
    {
        foreach (var player in _players)
        {
            if (player is not null && player.IsValid() && player.SteamId == steamId)
            {
                return player;
            }
        }

        return null;
    }

    public IGamePlayer[] GetAllPlayers(bool ignoreFakeClient = true)
    {
        var result = new List<IGamePlayer>();

        foreach (var player in _players)
        {
            if (player is null || !player.IsValid())
            {
                continue;
            }

            if (ignoreFakeClient && player.IsFakeClient)
            {
                continue;
            }

            // Ensure client is actually in a connected state
            if (player.Client.SignOnState >= SignOnState.Connected)
            {
                result.Add(player);
            }
        }

        return result.ToArray();
    }

    public IPlayerController? GetController(IGameClient client)
    {
        return GetController(client.Slot);
    }

    public IPlayerController? GetController(int slot)
    {
        var player = GetPlayer(slot);
        return player is GamePlayer gp ? gp.Controller : null;
    }

    public bool IsAdmin(SteamID steamId)
    {
        return config.AdminSteamIds.Contains(steamId.ToString());
    }

    #region Player Operations

    public IEnumerable<IPlayerController> GetPlayers()
    {
        return GetPlayerControllers();
    }

    public IEnumerable<IPlayerController> GetAlive()
    {
        return GetPlayerControllers().Where(x => x.GetPawn() is { IsAlive: true });
    }

    public IEnumerable<IPlayerController> GetCTs()
    {
        return GetPlayerControllers().Where(x => x.Team == CStrikeTeam.CT);
    }

    public IEnumerable<IPlayerController> GetTs()
    {
        return GetPlayerControllers().Where(x => x.Team == CStrikeTeam.TE);
    }

    public bool IsCtAlive()
    {
        return IsTeamAlive(CStrikeTeam.CT);
    }

    public bool IsTAlive()
    {
        return IsTeamAlive(CStrikeTeam.TE);
    }

    public bool IsTeamAlive(CStrikeTeam team)
    {
        foreach (var player in GetPlayers())
        {
            if (player.GetPawn() is not { } pawn)
            {
                continue;
            }

            if (!pawn.IsAlive)
            {
                continue;
            }

            if (pawn.Team == team)
            {
                return true;
            }
        }

        return false;
    }

    public IEnumerable<IPlayerController> GetSpecs()
    {
        return GetPlayerControllers().Where(x => x.Team == CStrikeTeam.Spectator);
    }

    private IEnumerable<IPlayerController> GetPlayerControllers()
    {
        // Return all valid player controllers
        foreach (var player in _players)
        {
            if (player is not null && player.IsValid() && player.Controller is not null)
            {
                yield return player.Controller;
            }
        }
    }

    #endregion

    #region IClientListener Implementation

    /// <summary>
    /// Called when a player is put in server (controller is available)
    /// Update the client reference and cache the controller
    /// </summary>
    private void OnClientPutInServer(IGameClient client)
    {
        if (!client.IsValid || client.IsHltv || client.IsFakeClient)
        {
            return;
        }

        if (client.Slot < 0 || client.Slot >= _players.Length)
        {
            return;
        }

        // Create or get existing player
        if (_players[client.Slot] is not { } player)
        {
            _players[client.Slot] = player = new GamePlayer(client, this);
            logger.LogDebug("Player connected: {name} (Slot: {slot}, SteamID: {steamId})",
                client.Name, client.Slot, client.SteamId);
        }
        else if (player.SteamId.ToString() != client.SteamId.ToString())
        {
            // Different player in same slot, create new
            _players[client.Slot] = player = new GamePlayer(client, this);
            logger.LogDebug("Player replaced in slot: {name} (Slot: {slot}, SteamID: {steamId})",
                client.Name, client.Slot, client.SteamId);
        }

        // Update the client reference
        player.UpdateClient(client);

        // Find and cache the player controller using the ControllerIndex
        var controller = bridge.EntityManager.FindEntityByIndex<IPlayerController>(client.ControllerIndex);
        if (controller != null)
        {
            player.SetController(controller);
            logger.LogDebug("Set controller for player {name} (Slot: {slot}, SteamID: {steamId})",
                client.Name, client.Slot, client.SteamId);
        }
    }

    /// <summary>
    /// Called when a player disconnects
    /// Clean up cached data for this player
    /// </summary>
    private void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
    {
        if (client.Slot < 0 || client.Slot >= _players.Length)
        {
            return;
        }

        if (_players[client.Slot] is not { } player || !player.Client.Equals(client))
        {
            return;
        }

        // Invalidate and clear the player
        player.Invalidate();
        _players[client.Slot] = null;

        logger.LogDebug("Cleared cache for slot {slot} (Reason: {reason})",
            client.Slot, reason);
    }

    #endregion
}