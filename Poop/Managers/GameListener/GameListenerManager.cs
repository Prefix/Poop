using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Sharp.Shared.Listeners;

namespace Prefix.Poop.Managers.GameListener;

/// <summary>
/// Manages game lifecycle events and exposes them as .NET events
/// </summary>
internal sealed class GameListenerManager(InterfaceBridge bridge) : IGameListenerManager, IManager, IGameListener
{
    public event IGameListenerManager.ListenerDelegate? GameInit;
    public event IGameListenerManager.ListenerDelegate? GameShutdown;
    public event IGameListenerManager.ListenerDelegate? GamePreShutdown;
    public event IGameListenerManager.ListenerDelegate? ResourcePrecache;
    public event IGameListenerManager.ListenerDelegate? RoundRestartPre;
    public event IGameListenerManager.ListenerDelegate? RoundRestartPost;
    public event IGameListenerManager.ListenerDelegate? ServerActive;

    public void OnServerActivate()
        => ServerActive?.Invoke();

    public void OnGamePreShutdown()
        => GamePreShutdown?.Invoke();

    public void OnGameInit()
        => GameInit?.Invoke();

    public void OnGameShutdown()
        => GameShutdown?.Invoke();

    public void OnResourcePrecache()
        => ResourcePrecache?.Invoke();

    public void OnRoundRestart()
        => RoundRestartPre?.Invoke();

    public void OnRoundRestarted()
        => RoundRestartPost?.Invoke();

    public bool Init()
    {
        bridge.ModSharp.InstallGameListener(this);

        return true;
    }

    public void Shutdown()
    {
        bridge.ModSharp.RemoveGameListener(this);
    }

    public int ListenerVersion => IGameListener.ApiVersion;
    public int ListenerPriority => 0;
}
