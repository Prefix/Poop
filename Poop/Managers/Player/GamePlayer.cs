using System;
using Prefix.Poop.Interfaces.Modules.Player;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Units;

namespace Prefix.Poop.Managers.Player;

/// <summary>
/// Implementation of IGamePlayer that wraps IGameClient
/// </summary>
internal class GamePlayer(IGameClient client, PlayerManager playerManager) : IGamePlayer
{
    private IGameClient _client = client;
    private IPlayerController? _controller;
    private bool _isValid = true;

    public IGameClient Client => _client;

    public bool IsAdmin => playerManager.IsAdmin(SteamId);

    public SteamID SteamId { get; } = client.SteamId;

    public string Name { get; } = client.Name;

    public int Slot { get; } = client.Slot;

    public IPlayerController? Controller => _controller;

    public bool IsFakeClient => _client.IsFakeClient;

    /// <summary>
    /// Update the client reference (e.g., when player is put in server)
    /// </summary>
    internal void UpdateClient(IGameClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Set the player controller
    /// </summary>
    internal void SetController(IPlayerController? controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// Mark this player as invalid (disconnected)
    /// </summary>
    internal void Invalidate()
    {
        _isValid = false;
        _controller = null;
    }

    /// <summary>
    /// Check if this player is still valid
    /// </summary>
    public bool IsValid()
    {
        return _isValid && _client.IsValid;
    }
}
