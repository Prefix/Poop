using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Prefix.Poop.Modules.PoopModule.Lifecycle;

/// <summary>
/// Manages rainbow-colored poops that cycle through colors dynamically
/// </summary>
internal sealed class RainbowPoopTracker : IRainbowPoopTracker
{
    private readonly ILogger<RainbowPoopTracker> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly IEventManager _eventManager;
    private readonly IConfigManager _config;

    // Track rainbow poop entities with their spawn time
    private readonly Dictionary<IBaseEntity, DateTime> _rainbowPoops = new();

    // Timer handle for rainbow color updates
    private Guid? _rainbowTimerHandle;

    // Update interval for rainbow colors (in seconds)
    private const double UpdateInterval = 0.1; // 10 times per second for smooth color transitions

    // Maximum age for rainbow poops (cleanup after this time)
    private const double MaxPoopAgeMinutes = 5.0; // Remove from tracking after 5 minutes

    // HSV color cycling state
    private float _currentHue = 0.0f;

    // HSV to RGB cache for performance optimization
    private static readonly Dictionary<int, Color32> _hsvCache = new();
    private static readonly object _cacheLock = new();

    public RainbowPoopTracker(ILogger<RainbowPoopTracker> logger, InterfaceBridge bridge, IEventManager eventManager, IConfigManager config)
    {
        _logger = logger;
        _bridge = bridge;
        _eventManager = eventManager;
        _config = config;

        // Listen to round_end event to clear tracked rainbow poops
        _eventManager.ListenEvent("round_end", OnRoundEnd);
    }

    public bool Init()
    {
        _logger.LogInformation("RainbowPoopTracker initialized - Update interval: {interval}s", UpdateInterval);
        return true;
    }

    public void OnAllSharpModulesLoaded()
    {
        _logger.LogDebug("RainbowPoopTracker: All modules loaded");
    }

    public void Shutdown()
    {
        _logger.LogInformation("RainbowPoopTracker shutting down - {count} rainbow poops tracked", _rainbowPoops.Count);
        StopRainbowTimer();
        _rainbowPoops.Clear();
        _currentHue = 0.0f; // Reset hue on shutdown
    }

    /// <summary>
    /// Handles round end event - clears all tracked rainbow poops
    /// </summary>
    private void OnRoundEnd(IGameEvent ev)
    {
        _logger.LogDebug("Round end - clearing {count} rainbow poops", _rainbowPoops.Count);
        ClearAll();
    }

    /// <summary>
    /// Tracks a rainbow poop entity for color cycling
    /// </summary>
    public void TrackRainbowPoop(IBaseEntity entity)
    {
        // Check if rainbow poops are enabled
        if (!_config.EnableRainbowPoops)
        {
            _logger.LogDebug("Rainbow poops are disabled, not tracking");
            return;
        }

        if (entity == null || !entity.IsValid())
        {
            _logger.LogWarning("Attempted to track invalid rainbow poop entity");
            return;
        }

        _logger.LogDebug("Tracking new rainbow poop entity (Handle: {handle})", entity.Handle);
        _rainbowPoops[entity] = DateTime.UtcNow;

        // Start the update timer if not already running
        StartRainbowTimerIfNeeded();
    }

    /// <summary>
    /// Stops tracking a specific rainbow poop
    /// </summary>
    public void StopTracking(IBaseEntity entity)
    {
        if (_rainbowPoops.Remove(entity))
        {
            _logger.LogDebug("Stopped tracking rainbow poop entity (Handle: {handle})", entity.Handle);

            // Stop timer if no more rainbow poops
            if (_rainbowPoops.Count == 0)
            {
                StopRainbowTimer();
            }
        }
    }

    /// <summary>
    /// Clears all tracked rainbow poops (e.g., on round end)
    /// </summary>
    public void ClearAll()
    {
        _logger.LogDebug("Clearing all {count} rainbow poops", _rainbowPoops.Count);
        _rainbowPoops.Clear();
        _currentHue = 0.0f; // Reset hue
        StopRainbowTimer();
    }

    /// <summary>
    /// Gets the number of currently tracked rainbow poops
    /// </summary>
    public int GetTrackedCount() => _rainbowPoops.Count;

    /// <summary>
    /// Starts the rainbow update timer if not already running
    /// </summary>
    private void StartRainbowTimerIfNeeded()
    {
        if (_rainbowTimerHandle.HasValue)
        {
            // Timer already running
            return;
        }

        _logger.LogInformation("Starting rainbow color update timer");
        _rainbowTimerHandle = _bridge.ModSharp.PushTimer(() =>
        {
            UpdateRainbowColors();
            return TimerAction.Continue; // Keep repeating
        }, UpdateInterval, GameTimerFlags.Repeatable | GameTimerFlags.StopOnRoundEnd);
    }

    /// <summary>
    /// Stops the rainbow update timer
    /// </summary>
    private void StopRainbowTimer()
    {
        if (_rainbowTimerHandle.HasValue)
        {
            _logger.LogInformation("Stopping rainbow color update timer");
            _bridge.ModSharp.StopTimer(_rainbowTimerHandle.Value);
            _rainbowTimerHandle = null;
        }
    }

    /// <summary>
    /// Updates colors for all tracked rainbow poops
    /// </summary>
    private void UpdateRainbowColors()
    {
        if (_rainbowPoops.Count == 0)
        {
            // No poops to update, stop the timer
            StopRainbowTimer();
            return;
        }

        // Calculate speed multiplier based on current hue for smooth color transitions
        // Use RainbowAnimationSpeed from config
        float speedMultiplier = GetSpeedMultiplierForHue(_currentHue);
        float hueIncrement = _config.RainbowAnimationSpeed * speedMultiplier;

        // Update global hue
        _currentHue += hueIncrement;
        if (_currentHue >= 360.0f)
            _currentHue = 0.0f;

        // Get cached RGB color for current hue
        var color = HsvToRgbCached(_currentHue, 1.0f, 1.0f);

        var now = DateTime.UtcNow;
        var entitiesToRemove = new List<IBaseEntity>();

        foreach (var kvp in _rainbowPoops)
        {
            var entity = kvp.Key;
            var spawnTime = kvp.Value;

            // Check if entity is still valid
            if (!entity.IsValid())
            {
                entitiesToRemove.Add(entity);
                continue;
            }

            // Check if entity is too old (cleanup old references)
            var age = (now - spawnTime).TotalMinutes;
            if (age > MaxPoopAgeMinutes)
            {
                _logger.LogDebug("Rainbow poop too old ({age:F1} min), removing from tracking", age);
                entitiesToRemove.Add(entity);
                continue;
            }

            try
            {
                // Cast to IBaseModelEntity to access RenderColor
                if (entity is IBaseModelEntity modelEntity)
                {
                    // Update the entity's color with cached color
                    modelEntity.RenderColor = color;

                    // Notify the game engine that the render color changed
                    // This forces the client to update the visual representation
                    if (entity is Sharp.Shared.CStrike.ISchemaObject schemaObj)
                    {
                        schemaObj.NetworkStateChanged("m_clrRender");
                    }
                }
                else
                {
                    _logger.LogWarning("Rainbow poop entity is not IBaseModelEntity, cannot update color");
                    entitiesToRemove.Add(entity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rainbow color for entity {handle}", entity.Handle);
                entitiesToRemove.Add(entity);
            }
        }

        // Remove invalid/old entities
        foreach (var entity in entitiesToRemove)
        {
            _rainbowPoops.Remove(entity);
        }

        // Stop timer if no more poops
        if (_rainbowPoops.Count == 0)
        {
            StopRainbowTimer();
        }
    }

    /// <summary>
    /// Gets speed multiplier based on current hue for smoother color transitions
    /// Slows down transition in certain hue ranges for better visual appeal
    /// </summary>
    private static float GetSpeedMultiplierForHue(float hue)
    {
        if (hue is >= 0 and < 60) // Red to Yellow
            return 0.6f; // Slow transition
        else if (hue is >= 60 and < 120) // Yellow to Green
            return 2.0f; // Medium-slow transition
        else if (hue is >= 120 and < 180) // Green to Cyan
            return 1.5f; // Medium-fast transition
        else if (hue is >= 180 and < 240) // Cyan to Blue
            return 1.5f; // Medium-slow transition
        else if (hue is >= 240 and < 300) // Blue to Magenta
            return 1.5f; // Very slow transition
        else if (hue is >= 300 and < 360) // Magenta to Red
            return 0.5f; // Fast transition
        else
            return 0.1f; // Default speed
    }

    /// <summary>
    /// Cached HSV to RGB conversion for performance
    /// Uses a cache to avoid recalculating the same colors repeatedly
    /// </summary>
    private static Color32 HsvToRgbCached(float h, float s, float v)
    {
        int hueKey = (int)Math.Round(h);

        lock (_cacheLock)
        {
            if (_hsvCache.TryGetValue(hueKey, out var cachedColor))
            {
                return cachedColor;
            }

            var color = HsvToRgb(h, s, v);
            _hsvCache[hueKey] = color;

            // Clear cache if it gets too large (shouldn't happen with 360 max keys)
            if (_hsvCache.Count > 360)
            {
                _hsvCache.Clear();
            }

            return color;
        }
    }

    /// <summary>
    /// Converts HSV color to RGB
    /// </summary>
    /// <param name="h">Hue (0-360)</param>
    /// <param name="s">Saturation (0-1)</param>
    /// <param name="v">Value/Brightness (0-1)</param>
    /// <returns>RGB color</returns>
    private static Color32 HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        float m = v - c;

        float r = 0, g = 0, b = 0;

        if (h >= 0 && h < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (h >= 60 && h < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (h >= 120 && h < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (h >= 180 && h < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (h >= 240 && h < 300)
        {
            r = x; g = 0; b = c;
        }
        else if (h >= 300 && h < 360)
        {
            r = c; g = 0; b = x;
        }

        return new Color32(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255),
            255
        );
    }
}
