using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.FlashingHtmlHudFix;

internal sealed class FlashingHtmlHudFixModule : IModule
{
    private readonly InterfaceBridge _bridge;
    private readonly IGameListenerManager _gameListener;

    private IGameRules? _gameRules;

    public FlashingHtmlHudFixModule(
        InterfaceBridge bridge,
        IGameListenerManager gameListener)
    {
        _bridge = bridge;
        _gameListener = gameListener;
        _gameListener.GameInit += OnGameInit;
    }

    public bool Init()
    {
        return true;
    }

    public void Shutdown()
    {
        _bridge.ModSharp.RemoveGameFrameHook(pre: null, post: OnGameFramePre);
        _gameListener.GameInit -= OnGameInit;
    }

    private void OnGameInit()
    {
        _gameRules = null;
        _bridge.ModSharp.InstallGameFrameHook(pre: null, post: OnGameFramePre);
    }

    private void OnGameFramePre(bool simulating, bool firstTick, bool lastTick)
    {
        _gameRules = _bridge.ModSharp.GetGameRules();
        _gameRules.IsGameRestart = _gameRules.RestartRoundTime == 0.0f;
    }
}
