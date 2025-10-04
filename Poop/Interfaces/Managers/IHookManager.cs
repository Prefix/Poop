using Prefix.Poop.Interfaces;

namespace Prefix.Poop.Interfaces.Managers;

internal interface IHookManager : IManager
{
    INativeVirtualHook CreateVirtual(string module, string classname, string method, nint detour);

    INativeDetourHook CreateDetour(string classname, string method, nint trampoline);

    INativeDetourHook CreateDetour(string key, nint trampoline);

    INativeMidFuncHook CreateMidHook(string key, nint trampoline);
}
