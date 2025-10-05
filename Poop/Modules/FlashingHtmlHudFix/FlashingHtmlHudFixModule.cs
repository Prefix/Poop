using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.FlashingHtmlHudFix;

internal sealed class FlashingHtmlHudFixModule : IModule
{
    private readonly InterfaceBridge _bridge;

    public FlashingHtmlHudFixModule(
        InterfaceBridge bridge)
    {
        _bridge = bridge;
    }

    public bool Init()
    {
        _bridge.ModSharp.InstallGameFrameHook(pre: null, post: OnGameFramePre);
        return true;
    }

    public void Shutdown()
    {
        _bridge.ModSharp.RemoveGameFrameHook(pre: null, post: OnGameFramePre);
    }

    private void OnGameFramePre(bool simulating, bool firstTick, bool lastTick)
    {
        _bridge.ModSharp.GetGameRules().IsGameRestart = _bridge.ModSharp.GetGameRules().RestartRoundTime == 0.0f;
    }
}
