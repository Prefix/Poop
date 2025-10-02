using System;
using System.Collections.Generic;
using Prefix.Poop.Interfaces;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Managers.Event;

internal class EventManager : IEventManager, IManager, IEventListener
{
    private readonly InterfaceBridge       _bridge;
    private readonly ILogger<EventManager> _logger;

    private readonly Dictionary<string, IEventManager.DelegateOnEventFired?>        _listeners;
    private readonly Dictionary<string, HashSet<IEventManager.DelegateOnHookEvent>> _hooks;
    private readonly HashSet<string>                                                _events;

    public EventManager(InterfaceBridge bridge, ILogger<EventManager> logger)
    {
        _bridge = bridge;
        _logger = logger;

        _events    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _hooks     = new Dictionary<string, HashSet<IEventManager.DelegateOnHookEvent>>(StringComparer.OrdinalIgnoreCase);
        _listeners = new Dictionary<string, IEventManager.DelegateOnEventFired?>(StringComparer.OrdinalIgnoreCase);
    }

    public bool Init()
    {
        _bridge.EventManager.InstallEventListener(this);

        return true;
    }

    public void Shutdown()
    {
        _bridge.EventManager.RemoveEventListener(this);
    }

    // For this module should use this EventManager instead Shared.Managers.EventManager
    int IEventListener.ListenerPriority => int.MaxValue;
    int IEventListener.ListenerVersion  => IEventListener.ApiVersion;

    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        var eventName = e.Name;

        if (!_hooks.TryGetValue(eventName, out var callbacks))
        {
            return true;
        }

        var param  = new EventHookParams(e, serverOnly);
        var result = EHookAction.Ignored;

        foreach (var callback in callbacks)
        {
            try
            {
                var ac = callback(param);

                if (ac.Action > result)
                {
                    result = ac.Action;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calling listener");
            }
        }

        // Block Event
        if (result == EHookAction.SkipCallReturnOverride)
        {
            return false;
        }

        // Allow Event
        if (result != EHookAction.Ignored)
        {
            serverOnly = param.ServerOnly;
        }

        return true;
    }

    public void FireGameEvent(IGameEvent e)
        => _listeners.GetValueOrDefault(e.Name)?.Invoke(e);

    public void HookEvent(string eventName, IEventManager.DelegateOnHookEvent callback)
    {
        if (_events.Add(eventName))
        {
            _bridge.EventManager.HookEvent(eventName);
        }

        if (!_hooks.ContainsKey(eventName))
        {
            _hooks[eventName] = [];
        }

        _hooks[eventName].Add(callback);
    }

    public void ListenEvent(string eventName, IEventManager.DelegateOnEventFired callback)
    {
        if (_events.Add(eventName))
        {
            _bridge.EventManager.HookEvent(eventName);
        }

        if (!_listeners.ContainsKey(eventName))
        {
            _listeners[eventName] = callback;
        }
        else
        {
            _listeners[eventName] += callback;
        }
    }

    public T? CreateEvent<T>(bool force) where T : class, IGameEvent
        => _bridge.EventManager.CreateEvent<T>(force);

    public IGameEvent? CreateEvent(string eventName, bool force)
        => _bridge.EventManager.CreateEvent(eventName, force);
}
