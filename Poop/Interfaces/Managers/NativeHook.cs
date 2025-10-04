namespace Prefix.Poop.Interfaces.Managers;

internal interface INativeHook
{
    bool IsAvailable();

    void Uninstall();

    bool Install();
}

internal interface INativeDetourHook : INativeHook
{
    nint Trampoline { get; }
}

internal interface INativeVirtualHook : INativeHook
{
    nint Trampoline { get; }
}

internal interface INativeMidFuncHook : INativeHook;
