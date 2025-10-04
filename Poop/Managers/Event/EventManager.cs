using System;
using System.Collections.Generic;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Microsoft.Extensions.Logging;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Managers.Event;

internal class EventManager(InterfaceBridge bridge, ILogger<EventManager> logger) : IEventManager, IEventListener
{
    private readonly Dictionary<string, IEventManager.DelegateOnEventFired?> _listeners = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<IEventManager.DelegateOnHookEvent>> _hooks = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _events = new(StringComparer.OrdinalIgnoreCase);

    public bool Init()
    {
        bridge.EventManager.InstallEventListener(this);

        return true;
    }

    public void Shutdown()
    {
        bridge.EventManager.RemoveEventListener(this);
    }

    // For this module should use this EventManager instead Shared.Managers.EventManager
    int IEventListener.ListenerPriority => int.MaxValue;
    int IEventListener.ListenerVersion => IEventListener.ApiVersion;

    public bool HookFireEvent(IGameEvent e, ref bool serverOnly)
    {
        var eventName = e.Name;

        if (!_hooks.TryGetValue(eventName, out var callbacks))
        {
            return true;
        }

        var param = new EventHookParams(e, serverOnly);
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
                logger.LogError(ex, "An error occurred while calling listener");
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
            bridge.EventManager.HookEvent(eventName);
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
            bridge.EventManager.HookEvent(eventName);
        }

        if (_listeners.TryAdd(eventName, callback))
        {
            return;
        }

        _listeners[eventName] += callback;
    }

    public T? CreateEvent<T>(bool force) where T : class, IGameEvent
        => bridge.EventManager.CreateEvent<T>(force);

    public IGameEvent? CreateEvent(string eventName, bool force)
        => bridge.EventManager.CreateEvent(eventName, force);

    /// <summary>
    /// Print a message to a player's center HTML HUD using the survival respawn status overlay
    /// This is a cleaner alternative to the CenterHtmlMenu system
    /// </summary>
    public void PrintToCenterHtml(IPlayerController controller, string message, int duration = 5)
    {
        logger.LogDebug($"[PrintToCenterHtml] Attempting to send message to {controller.PlayerName} (SteamID: {controller.SteamId})");
        logger.LogDebug($"[PrintToCenterHtml] Message: {message}, Duration: {duration}");

        // Create the show_survival_respawn_status event
        if (bridge.EventManager.CreateEvent("show_survival_respawn_status", true) is not { } e)
        {
            logger.LogWarning("Failed to create show_survival_respawn_status event for PrintToCenterHtml");
            return;
        }

        // Get the game client and validate
        if (bridge.ClientManager.GetGameClient(controller.SteamId) is not
            {
                IsValid: true,
                IsFakeClient: false
            }
            client)
        {
            logger.LogWarning($"[PrintToCenterHtml] Failed to get valid client for SteamID: {controller.SteamId}");
            e.Dispose();
            return;
        }

        // Set event properties and fire to client
        e.SetString("loc_token", message);
        e.SetInt("duration", duration);
        e.FireToClient(client);
        e.Dispose();

        logger.LogDebug($"[PrintToCenterHtml] Successfully sent message to {controller.PlayerName}");
    }
}
