using Prefix.Poop.Interfaces;

namespace Prefix.Poop.Managers.Hook;

internal interface IHookManager
{
    INativeVirtualHook CreateVirtual(string module, string classname, string method, nint detour);

    INativeDetourHook CreateDetour(string classname, string method, nint trampoline);

    INativeDetourHook CreateDetour(string key, nint trampoline);

    INativeMidFuncHook CreateMidHook(string key, nint trampoline);
}
