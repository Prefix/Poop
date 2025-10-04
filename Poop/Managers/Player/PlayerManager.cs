using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Modules.Player;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Prefix.Poop.Managers.Player;

internal class PlayerManager(
    InterfaceBridge bridge,
    ILogger<PlayerManager> logger,
    ConfigManager config)
    : IPlayerManager, IClientListener
{
    private readonly GamePlayer?[] _players = new GamePlayer?[PlayerSlot.MaxPlayerSlot];

    public bool Init()
    {
        bridge.ClientManager.InstallClientListener(this);
        logger.LogInformation("PlayerManager initialized with {count} admins", config.AdminSteamIds.Count);
        return true;
    }

    public void OnPostInit()
    {
    }

    public void OnAllSharpModulesLoaded()
    {
    }

    public void Shutdown()
    {
        bridge.ClientManager.RemoveClientListener(this);

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

    public IGamePlayer? GetPlayerBySteamId(string steamId)
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

    public bool IsAdmin(string steamId)
    {
        return config.AdminSteamIds.Contains(steamId);
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
    /// Called when a client connects (early stage)
    /// Create or validate the GamePlayer instance
    /// </summary>
    public void OnClientConnected(IGameClient client)
    {
        if (!client.IsValid || client.IsHltv)
        {
            return;
        }

        if (client.Slot < 0 || client.Slot >= _players.Length)
        {
            logger.LogWarning("Invalid slot {slot} for client {name}", client.Slot, client.Name);
            return;
        }

        // Check for double connection with same slot
        if (_players[client.Slot] is { } old)
        {
            if (old.Client.Equals(client) && old.SteamId == client.SteamId.ToString())
            {
                logger.LogWarning("Double connection with same slot. old: {old}, new: {new}",
                    old.Client, client);
                return;
            }
        }

        _players[client.Slot] = new GamePlayer(client, this);

        logger.LogDebug("Player connected: {name} (Slot: {slot}, SteamID: {steamId})",
            client.Name, client.Slot, client.SteamId);
    }

    /// <summary>
    /// Called when a player is put in server (controller is available)
    /// Update the client reference and cache the controller
    /// </summary>
    public void OnClientPutInServer(IGameClient client)
    {
        if (!client.IsValid || client.IsHltv || client.IsFakeClient)
        {
            return;
        }

        if (client.Slot < 0 || client.Slot >= _players.Length)
        {
            return;
        }

        if (_players[client.Slot] is not { } player || player.SteamId != client.SteamId.ToString())
        {
            return;
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
    /// Called after admin check (player is authorized)
    /// PoopPlayerManager will handle loading color preferences
    /// </summary>
    public void OnClientPostAdminCheck(IGameClient client)
    {
        // Color preference loading is handled by PoopPlayerManager module
    }

    /// <summary>
    /// Called when a player disconnects
    /// Clean up cached data for this player
    /// </summary>
    public void OnClientDisconnected(IGameClient client, NetworkDisconnectionReason reason)
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

    public int ListenerVersion => IClientListener.ApiVersion;
    public int ListenerPriority => 0;

    #endregion
}