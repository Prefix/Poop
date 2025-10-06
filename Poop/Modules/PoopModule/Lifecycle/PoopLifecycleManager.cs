using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopModule.Lifecycle;

/// <summary>
/// Manages poop entity lifecycle including tracking, lifetime timers, and cleanup
/// </summary>
internal sealed class PoopLifecycleManager : IPoopLifecycleManager
{
    private readonly ILogger<PoopLifecycleManager> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly IConfigManager _config;

    // Track poops with their removal timer IDs
    private readonly Dictionary<IBaseEntity, Guid> _poopTimers = new();

    // Track poops spawned this round (for max limit)
    private int _poopsThisRound;

    public PoopLifecycleManager(
        ILogger<PoopLifecycleManager> logger,
        InterfaceBridge bridge,
        IEventManager eventManager,
        IConfigManager config)
    {
        _logger = logger;
        _bridge = bridge;
        _config = config;

        // Listen to round events for cleanup
        eventManager.ListenEvent("round_start", OnRoundStart);
        eventManager.ListenEvent("round_end", OnRoundEnd);
    }

    public bool Init()
    {
        return true;
    }

    public void Shutdown()
    {
        RemoveAllPoops();
    }

    /// <summary>
    /// Tracks a poop entity and schedules removal if lifetime is configured
    /// </summary>
    public void TrackPoop(IBaseEntity entity)
    {
        if (!entity.IsValid())
        {
            _logger.LogWarning("Attempted to track invalid poop entity");
            return;
        }

        // Increment round counter
        _poopsThisRound++;

        // Schedule removal if lifetime is configured (> 0)
        if (_config.PoopLifetimeSeconds > 0)
        {
            var timerId = _bridge.ModSharp.PushTimer(() =>
            {
                RemovePoopInternal(entity);
                return TimerAction.Stop;
            }, _config.PoopLifetimeSeconds, GameTimerFlags.StopOnRoundEnd);

            _poopTimers[entity] = timerId;

        }
        else
        {
            // Track without timer (lives forever, but track for cleanup on round end)
            _poopTimers[entity] = Guid.Empty;
        }
    }

    /// <summary>
    /// Manually removes a tracked poop entity
    /// </summary>
    public void RemovePoop(IBaseEntity entity)
    {
        RemovePoopInternal(entity);
    }

    /// <summary>
    /// Removes all tracked poop entities
    /// </summary>
    public void RemoveAllPoops()
    {
        var count = _poopTimers.Count;

        foreach (var (entity, timerId) in _poopTimers.ToList())
        {
            // Stop timer if it exists
            if (timerId != Guid.Empty && _bridge.ModSharp.IsValidTimer(timerId))
            {
                _bridge.ModSharp.StopTimer(timerId);
            }

            // Remove entity
            if (entity.IsValid())
            {
                try
                {
                    entity.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error removing poop entity");
                }
            }
        }

        _poopTimers.Clear();
    }

    /// <summary>
    /// Gets the current count of tracked poops
    /// </summary>
    public int GetTrackedPoopCount()
    {
        return _poopTimers.Count;
    }

    /// <summary>
    /// Checks if max poops per round limit has been reached
    /// </summary>
    public bool HasReachedMaxPoopsPerRound()
    {
        // 0 means no limit
        if (_config.MaxPoopsPerRound <= 0)
            return false;

        return _poopsThisRound >= _config.MaxPoopsPerRound;
    }

    /// <summary>
    /// Internal method to remove a poop and clean up its timer
    /// </summary>
    private void RemovePoopInternal(IBaseEntity? entity)
    {
        if (entity == null || !_poopTimers.TryGetValue(entity, out var timerId))
            return;

        // Stop timer if it exists and is valid
        if (timerId != Guid.Empty && _bridge.ModSharp.IsValidTimer(timerId))
        {
            _bridge.ModSharp.StopTimer(timerId);
        }

        // Remove entity
        if (entity.IsValid())
        {
            try
            {
                entity.Kill();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing poop entity");
            }
        }

        // Remove from tracking
        _poopTimers.Remove(entity);
    }

    /// <summary>
    /// Handles round start - resets round poop counter
    /// </summary>
    private void OnRoundStart(IGameEvent ev)
    {
        _poopsThisRound = 0;
    }

    /// <summary>
    /// Handles round end - optionally removes all poops
    /// </summary>
    private void OnRoundEnd(IGameEvent ev)
    {
        if (_config.RemovePoopsOnRoundEnd)
        {
            RemoveAllPoops();
        }
    }
}
