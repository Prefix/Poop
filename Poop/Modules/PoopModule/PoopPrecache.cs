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
        return true;
    }

    /// <summary>
    /// Precaches poop model and sound events when resources are being loaded
    /// </summary>
    private void OnResourcePrecache()
    {

        try
        {
            // Precache the poop model
            if (!string.IsNullOrEmpty(_config.PoopModel))
            {
                _bridge.ModSharp.PrecacheResource(_config.PoopModel);
            }

            // Precache sound events file
            if (_config.EnableSounds && !string.IsNullOrEmpty(_config.SoundEventsFile))
            {
                _bridge.ModSharp.PrecacheResource(_config.SoundEventsFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to precache poop resources");
        }
    }
}
