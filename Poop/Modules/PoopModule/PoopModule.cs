using Prefix.Poop.Interfaces;
using Prefix.Poop.Managers.Event;
using Microsoft.Extensions.Logging;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Main Poop module handling game events and player interactions
/// </summary>
internal sealed class PoopModule : IModule
{
    private readonly ILogger<PoopModule> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly IEventManager _eventManager;
    private readonly PoopModuleConfig _config;

    public PoopModule(
        ILogger<PoopModule> logger, 
        InterfaceBridge bridge, 
        IEventManager eventManager,
        PoopModuleConfig config)
    {
        _logger = logger;
        _bridge = bridge;
        _eventManager = eventManager;
        _config = config;

        // Register game event listeners
        RegisterEvents();
        
        // Register native listeners
        RegisterListeners();
    }

    public bool Init()
    {
        _logger.LogInformation("PoopModule initialized");
        _logger.LogInformation("Configuration: Model={Model}, MinSize={Min}, MaxSize={Max}, Cooldown={Cooldown}s",
            _config.PoopModel, _config.MinPoopSize, _config.MaxPoopSize, _config.CommandCooldownSeconds);
        
        if (_config.DebugMode)
        {
            _logger.LogInformation("DEBUG MODE ENABLED");
        }
        
        return true;
    }

    public void OnPostInit()
    {
        _logger.LogInformation("PoopModule post-initialized");
    }

    public void Shutdown()
    {
        _logger.LogInformation("PoopModule shutting down");
    }

    private void RegisterEvents()
    {
        // Listen to game events
        _eventManager.ListenEvent("player_death", OnPlayerDeath);
        _eventManager.ListenEvent("round_start", OnRoundStart);
        _eventManager.ListenEvent("player_disconnect", OnPlayerDisconnect);
        _eventManager.ListenEvent("round_end", OnRoundEnd);
    }

    private static void RegisterListeners()
    {
        // Native listeners would be registered through the bridge
        // Note: ListenerHandler attributes are typically for CounterStrikeSharp
        // For ModSharp, you may need to use the bridge's listener registration
        
        // Example:
        // _bridge.ListenerManager.RegisterListener(Listeners.OnClientPutInServer, OnClientPutInServer);
        // _bridge.ListenerManager.RegisterListener(Listeners.OnMapStart, OnMapStart);
        // _bridge.ListenerManager.RegisterListener(Listeners.OnServerPrecacheResources, OnServerPrecacheResources);
    }

    #region Game Event Handlers

    /// <summary>
    /// Handles player death events
    /// Stores dead player position for poop placement
    /// </summary>
    private void OnPlayerDeath(IGameEvent ev)
    {
        if (ev is not IEventPlayerDeath e)
        {
            return;
        }

        _logger.LogInformation("Player death: {victim} killed by {killer}",
            e.VictimController?.PlayerName ?? "Unknown",
            e.KillerController?.PlayerName ?? "World");

        // TODO: Store dead player position
        // TODO: Store player name for poop placement
        
        // Implementation placeholder:
        // - Get victim's position
        // - Store in dictionary with player as key
        // - Position will be used for poop spawning commands
    }

    /// <summary>
    /// Handles round start events
    /// Clears dead player positions and cooldowns
    /// </summary>
    private void OnRoundStart(IGameEvent ev)
    {
        // TODO: Check actual event type when implementing
        // if (ev is not IEventRoundStart e) { return; }

        _logger.LogInformation("Round started: {eventName}", ev.Name);

        // TODO: Clear dead players dictionary
        // TODO: Clear player cooldowns
        // TODO: Clear rainbow poops list
        
        // Implementation placeholder:
        // - Clear all tracked dead player positions
        // - Reset command cooldowns
        // - Remove all active rainbow poop entities
    }

    /// <summary>
    /// Handles player disconnect events
    /// Cleanup player-specific data
    /// </summary>
    private void OnPlayerDisconnect(IGameEvent ev)
    {
        // TODO: Check actual event type and extract player info when implementing
        // if (ev is not IEventPlayerDisconnect e) { return; }

        _logger.LogInformation("Player disconnected: {eventName}", ev.Name);

        // TODO: Remove player from dead players dictionary
        // TODO: Remove player cooldowns
        // TODO: Remove player's poop color preferences
        
        // Implementation placeholder:
        // - Clean up player from deadPlayers dictionary
        // - Remove from cooldown tracking
        // - Clean up any player-specific state
    }

    /// <summary>
    /// Handles round end events
    /// </summary>
    private void OnRoundEnd(IGameEvent ev)
    {
        // TODO: Check actual event type when implementing
        // if (ev is not IEventRoundEnd e) { return; }

        _logger.LogInformation("Round ended: {eventName}", ev.Name);

        // TODO: Optional round end cleanup
        
        // Implementation placeholder:
        // - Could display poop statistics
        // - Could clean up some state (though round_start usually handles this)
    }

    #endregion

    #region Native Listener Handlers

    /// <summary>
    /// Called when a client connects to the server
    /// </summary>
    private void OnClientPutInServer(int playerSlot)
    {
        _logger.LogInformation("Client connected: slot {slot}", playerSlot);

        // TODO: Initialize player data
        // TODO: Load player poop preferences from database
        
        // Implementation placeholder:
        // - Get player from slot
        // - Validate player is not bot
        // - Load color preferences
        // - Initialize player-specific state
    }

    /// <summary>
    /// Called when map starts
    /// </summary>
    private void OnMapStart(string mapName)
    {
        _logger.LogInformation("Map started: {map}", mapName);

        // TODO: Store current map name
        // TODO: Initialize map-specific resources
        
        // Implementation placeholder:
        // - Store map name in field
        // - Could load map-specific poop configurations
    }

    /// <summary>
    /// Called during server resource precaching
    /// </summary>
    private void OnServerPrecacheResources(/* ResourceManifest manifest */)
    {
        _logger.LogInformation("Precaching poop resources: {model}", _config.PoopModel);

        // TODO: Precache poop model from config
        // TODO: Precache sound events from config
        
        // Implementation placeholder:
        // - Add poop model to manifest from _config.PoopModel
        // - Add sound events to manifest from _config.PoopSounds
        // manifest.AddResource(_config.PoopModel);
        // foreach (var sound in _config.PoopSounds)
        // {
        //     manifest.AddResource($"soundevents/{sound}.vsndevts");
        // }
    }

    #endregion
}
