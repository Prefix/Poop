using System;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Microsoft.Extensions.Logging;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Handles precaching of poop model and sound event resources
/// </summary>
internal sealed class PoopPrecache : IModule
{
    private readonly ILogger<PoopPrecache> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly IGameListenerManager _gameListener;
    private readonly IConfigManager _config;

    public PoopPrecache(
        ILogger<PoopPrecache> logger,
        InterfaceBridge bridge,
        IGameListenerManager gameListener,
        IConfigManager config)
    {
        _logger = logger;
        _bridge = bridge;
        _gameListener = gameListener;
        _config = config;

        // Subscribe to resource precache event
        _gameListener.ResourcePrecache += OnResourcePrecache;
    }

    public bool Init()
    {
        _logger.LogInformation("PoopPrecache initialized");
        return true;
    }

    public void OnPostInit()
    {
        // No post-init needed
    }

    public void Shutdown()
    {
        _logger.LogInformation("PoopPrecache shutting down");
    }

    /// <summary>
    /// Precaches poop model and sound events when resources are being loaded
    /// </summary>
    private void OnResourcePrecache()
    {
        _logger.LogInformation("Precaching poop resources...");

        try
        {
            // Precache the poop model
            if (!string.IsNullOrEmpty(_config.PoopModel))
            {
                _bridge.ModSharp.PrecacheResource(_config.PoopModel);
                _logger.LogInformation("Precached model: {model}", _config.PoopModel);
            }
            else
            {
                _logger.LogWarning("PoopModel is not configured");
            }

            // Precache sound events file
            if (_config.EnableSounds && !string.IsNullOrEmpty(_config.SoundEventsFile))
            {
                _bridge.ModSharp.PrecacheResource(_config.SoundEventsFile);
                _logger.LogInformation("Precached sound events: {soundEvents}", _config.SoundEventsFile);
            }
            else if (_config.EnableSounds)
            {
                _logger.LogWarning("Sounds are enabled but SoundEventsFile is not configured");
            }

            _logger.LogInformation("Poop resource precaching completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to precache poop resources");
        }
    }
}
