using System;
using System.Collections.Generic;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Hooks;

namespace Prefix.Poop.Managers.Hook;

internal class HookManager : IHookManager, IManager
{
    private readonly ILogger<HookManager> _logger;
    private readonly InterfaceBridge _bridge;

    private readonly List<INativeHook> _hooks;

    public HookManager(ILogger<HookManager> logger, InterfaceBridge bridge)
    {
        _logger = logger;
        _bridge = bridge;

        _hooks = [];
    }

    public bool Init()
        => true;

    public void Shutdown()
    {
        _hooks.ForEach(x =>
        {
            x.Uninstall();
            _logger.LogDebug("Shutdown NativeHook -> {x}", x.GetType().FullName);
        });
    }

    public INativeVirtualHook CreateVirtual(string module, string classname, string method, nint trampoline)
    {
        var hook = new VirtualHook(_bridge.HookManager.CreateVirtualHook(),
                                   module,
                                   classname,
                                   method,
                                   trampoline);

        _hooks.Add(hook);

        return hook;
    }

    public INativeDetourHook CreateDetour(string classname, string method, nint trampoline)
    {
        var address = _bridge.GameData.GetAddress(classname, method);

        return CreateDetour(address, trampoline);
    }

    public INativeDetourHook CreateDetour(string key, nint trampoline)
    {
        var address = _bridge.GameData.GetAddress(key);

        return CreateDetour(address, trampoline);
    }

    public INativeMidFuncHook CreateMidHook(string key, nint trampoline)
    {
        var hook = new MidFuncHook(_bridge.HookManager.CreateMidFuncHook(),
                                   _bridge.GameData.GetAddress(key),
                                   trampoline);

        _hooks.Add(hook);

        return hook;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    private INativeDetourHook CreateDetour(nint address, nint trampoline)
    {
        var hook = new DetourHook(_bridge.HookManager.CreateDetourHook(), address, trampoline);

        _hooks.Add(hook);

        return hook;
    }

    // You can implement abstract classes to make code reusable.
    // For example, here we just copypasta

    private sealed class VirtualHook : INativeVirtualHook
    {
        private readonly IVirtualHook _hook;
        private readonly string _module;
        private readonly string _class;
        private readonly string _function;
        private readonly nint _detour;

        private bool _installed;
        private bool _disposed;

        public VirtualHook(IVirtualHook hook, string module, string @class, string function, nint detour)
        {
            _hook = hook;
            _module = module;
            _class = @class;
            _function = function;
            _detour = detour;
        }

        public bool IsAvailable()
            => _installed && !_disposed;

        public bool Install()
        {
            if (_installed)
            {
                return true;
            }

            _hook.Prepare(_module, _class, _function, _detour);

            _installed = _hook.Install();

            return _installed;
        }

        public void Uninstall()
        {
            ObjectDisposedException.ThrowIf(_disposed, _hook);

            if (_installed)
            {
                _hook.Uninstall();
                _installed = false;
            }

            _disposed = true;
        }

        public nint Trampoline
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, _hook);

                return _installed ? _hook.Trampoline : throw new InvalidOperationException("Hook is not installed.");
            }
        }
    }

    private sealed class DetourHook : INativeDetourHook
    {
        private readonly IDetourHook _hook;
        private readonly nint _address;
        private readonly nint _detour;

        private bool _installed;
        private bool _disposed;

        public DetourHook(IDetourHook hook, nint address, nint detour)
        {
            _hook = hook;
            _address = address;
            _detour = detour;
        }

        public bool IsAvailable()
            => _installed && !_disposed;

        public bool Install()
        {
            if (_installed)
            {
                return true;
            }

            _hook.Prepare(_address, _detour);
            _installed = _hook.Install();

            return _installed;
        }

        public void Uninstall()
        {
            ObjectDisposedException.ThrowIf(_disposed, _hook);

            if (_installed)
            {
                _hook.Uninstall();
                _installed = false;
            }

            _disposed = true;
        }

        public nint Trampoline
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, _hook);

                return _installed ? _hook.Trampoline : throw new InvalidOperationException("Hook is not installed.");
            }
        }
    }

    private sealed class MidFuncHook : INativeMidFuncHook
    {
        private readonly IMidFuncHook _hook;
        private readonly nint _address;
        private readonly nint _detour;

        private bool _installed;
        private bool _disposed;

        public MidFuncHook(IMidFuncHook hook, nint address, nint detour)
        {
            _hook = hook;
            _address = address;
            _detour = detour;
        }

        public bool IsAvailable()
            => _installed && !_disposed;

        public bool Install()
        {
            if (_installed)
            {
                return true;
            }

            _hook.Prepare(_address, _detour);
            _installed = _hook.Install();

            return _installed;
        }

        public void Uninstall()
        {
            ObjectDisposedException.ThrowIf(_disposed, _hook);

            if (_installed)
            {
                _hook.Uninstall();
                _installed = false;
            }

            _disposed = true;
        }
    }
}
