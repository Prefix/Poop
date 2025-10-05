using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Shared;
using Prefix.Poop.Shared.Events;
using Prefix.Poop.Shared.Models;
using Sharp.Shared;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Example.PoopPlugin;

/// <summary>
/// Example plugin demonstrating how to use the Poop.Shared API
/// This plugin shows various ways to interact with the Poop plugin:
/// - Listening to poop events
/// - Spawning custom poops
/// - Getting player statistics
/// - Managing player color preferences
/// </summary>
public sealed class PoopExample : IModSharpModule
{
    public string DisplayName => "Poop Example Plugin";
    public string DisplayAuthor => "Prefix";

    private readonly ILogger<PoopExample> _logger;
    private readonly ISharedSystem _shared;
    private IPoopShared? _poopApi;

    public PoopExample(
        ISharedSystem sharedSystem,
        string? dllPath,
        string? sharpPath,
        Version? version,
        IConfiguration? coreConfiguration,
        bool hotReload)
    {
        _logger = sharedSystem.GetLoggerFactory().CreateLogger<PoopExample>();
        _shared = sharedSystem;
        
        _logger.LogInformation("PoopExample plugin loaded!");
    }

    public bool Init()
    {
        _logger.LogInformation("PoopExample initializing...");
        return true;
    }

    public void PostInit()
    {
        _logger.LogInformation("PoopExample post-initializing...");

        // Register example commands
        _shared.GetClientManager().InstallCommandCallback("forcepoop", OnForcePoopCommand);
        _shared.GetClientManager().InstallCommandCallback("poopstats", OnPoopStatsCommand);
        _shared.GetClientManager().InstallCommandCallback("randompoop", OnRandomPoopCommand);
        _shared.GetClientManager().InstallCommandCallback("massivepoop", OnMassivePoopCommand);

        _logger.LogInformation("PoopExample commands registered!");
    }

    public void Shutdown()
    {
        _logger.LogInformation("PoopExample shutting down...");

        // Unsubscribe from events
        if (_poopApi != null)
        {
            _poopApi.OnPoopSpawned -= OnPoopSpawned;
            _poopApi.OnPoopCommand -= OnPoopCommand;
        }

        // Unregister commands
        _shared.GetClientManager().RemoveCommandCallback("forcepoop", OnForcePoopCommand);
        _shared.GetClientManager().RemoveCommandCallback("poopstats", OnPoopStatsCommand);
        _shared.GetClientManager().RemoveCommandCallback("randompoop", OnRandomPoopCommand);
        _shared.GetClientManager().RemoveCommandCallback("massivepoop", OnMassivePoopCommand);
    }

    public void OnAllModulesLoaded()
    {
        _logger.LogInformation("All modules loaded - attempting to connect to Poop API...");

        try
        {
            // Get the Poop API interface wrapper
            var manager = _shared.GetSharpModuleManager();
            var poopInterface = manager.GetRequiredSharpModuleInterface<IPoopShared>(IPoopShared.Identity);
            
            if (poopInterface == null)
            {
                _logger.LogError("Could not find Poop plugin! Make sure Poop.dll is loaded.");
                return;
            }

            // Get the actual instance from the wrapper
            _poopApi = poopInterface.Instance;
            
            if (_poopApi == null)
            {
                _logger.LogWarning("Poop API instance is not available!");
                return;
            }

            _logger.LogInformation("Successfully connected to Poop API!");

            // Subscribe to events
            _poopApi.OnPoopSpawned += OnPoopSpawned;
            _poopApi.OnPoopCommand += OnPoopCommand;

            _logger.LogInformation("Subscribed to Poop events!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Poop API connection");
            _poopApi = null;
        }
    }

    #region Commands

    /// <summary>
    /// Command: !forcepoop
    /// Forces a player to spawn a poop at their location
    /// </summary>
    private ECommandAction OnForcePoopCommand(IGameClient client, StringCommand command)
    {
        if (_poopApi == null)
        {
            PrintToChat(client, "Poop API is not available!");
            return ECommandAction.Stopped;
        }

        _logger.LogInformation("{player} used !forcepoop command", client.Name);

        try
        {
            // Force spawn a poop with default parameters
            var result = _poopApi.ForcePlayerPoop(
                player: client,
                size: -1.0f, // -1 for random size
                color: null, // null to use player's preference
                playSounds: true
            );

            if (result != null)
            {
                PrintToChat(client, $"Forced poop spawned! Size: {result.Size:F3}");
            }
            else
            {
                PrintToChat(client, "Failed to spawn poop!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing poop for {player}", client.Name);
            PrintToChat(client, "An error occurred!");
        }

        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: !poopstats
    /// Shows the player's poop statistics
    /// </summary>
    private ECommandAction OnPoopStatsCommand(IGameClient client, StringCommand command)
    {
        if (_poopApi == null)
        {
            PrintToChat(client, "Poop API is not available!");
            return ECommandAction.Stopped;
        }

        _logger.LogInformation("{player} used !poopstats command", client.Name);

        // Use async pattern with fire-and-forget
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var stats = await _poopApi.GetPlayerStatsAsync(client.SteamId);

                if (stats != null)
                {
                    // Schedule response on main thread
                    await _shared.GetModSharp().InvokeFrameActionAsync(() =>
                    {
                        PrintToChat(client, $"=== Your Poop Stats ===");
                        PrintToChat(client, $"Poops Placed: {stats.PoopsPlaced}");
                        PrintToChat(client, $"Times Pooped On: {stats.TimesPoopedOn}");
                    });
                }
                else
                {
                    await _shared.GetModSharp().InvokeFrameActionAsync(() =>
                    {
                        PrintToChat(client, "No poop statistics found!");
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting poop stats for {player}", client.Name);
            }
        });

        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: !randompoop
    /// Spawns a poop with random color at player's location
    /// </summary>
    private ECommandAction OnRandomPoopCommand(IGameClient client, StringCommand command)
    {
        if (_poopApi == null)
        {
            PrintToChat(client, "Poop API is not available!");
            return ECommandAction.Stopped;
        }

        _logger.LogInformation("{player} used !randompoop command", client.Name);

        try
        {
            // Create a random color preference
            var random = new Random();
            var randomColor = new PoopColorPreference(
                red: random.Next(0, 256),
                green: random.Next(0, 256),
                blue: random.Next(0, 256)
            );

            // Spawn poop at player position with random color
            var result = _poopApi.ForcePlayerPoop(
                player: client,
                size: -1.0f, // random size
                color: randomColor,
                playSounds: true
            );

            if (result != null)
            {
                PrintToChat(client, $"Random poop spawned! RGB({randomColor.Red},{randomColor.Green},{randomColor.Blue}) Size: {result.Size:F3}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error spawning random poop for {player}", client.Name);
        }

        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: !massivepoop
    /// Spawns a massive poop (admin only)
    /// </summary>
    private ECommandAction OnMassivePoopCommand(IGameClient client, StringCommand command)
    {
        if (_poopApi == null)
        {
            PrintToChat(client, "Poop API is not available!");
            return ECommandAction.Stopped;
        }

        _logger.LogInformation("{player} used !massivepoop command", client.Name);

        try
        {
            // Spawn a massive golden poop
            var goldenColor = new PoopColorPreference(255, 215, 0); // Gold color

            var result = _poopApi.ForcePlayerPoop(
                player: client,
                size: 2.5f, // Massive size
                color: goldenColor,
                playSounds: true
            );

            if (result != null)
            {
                PrintToChat(client, $"MASSIVE GOLDEN POOP SPAWNED! Size: {result.Size:F3}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error spawning massive poop for {player}", client.Name);
        }

        return ECommandAction.Stopped;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper method to send a chat message to a player
    /// </summary>
    private void PrintToChat(IGameClient client, string message)
    {
        try
        {
            var entityManager = _shared.GetEntityManager();
            var controller = entityManager.FindPlayerControllerBySlot(client.Slot);
            if (controller == null)
            {
                _logger.LogWarning("Could not find controller for player {player}", client.Name);
                return;
            }
            controller.Print(HudPrintChannel.Chat, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing to chat for {player}", client.Name);
            // Fallback to console if chat fails
            PrintToChat(client, message);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Called whenever a poop is spawned in the game
    /// </summary>
    private void OnPoopSpawned(PoopSpawnedEventArgs args)
    {
        _logger.LogInformation(
            "Poop spawned by {player}: Size={size:F3}, Success={success}, HasVictim={hasVictim}",
            args.Player.Name,
            args.Size,
            args.Success,
            args.Victim != null
        );

        // Example: Reward players for massive poops
        if (args.Size >= 2.5f)
        {
            _logger.LogInformation("LEGENDARY POOP detected from {player}!", args.Player.Name);
            
            // You could give rewards, achievements, etc. here
            // For example, you could integrate with an economy plugin
        }

        // Example: Track command-triggered poops vs API poops
        if (args.IsCommandTriggered)
        {
            _logger.LogInformation("Player {player} used a command to spawn poop", args.Player.Name);
        }
    }

    /// <summary>
    /// Called before a player executes the poop command
    /// Can be used to block or allow the command
    /// </summary>
    private void OnPoopCommand(PoopCommandEventArgs args)
    {
        _logger.LogInformation("Player {player} is attempting to use poop command: {command}", 
            args.Player.Name, 
            args.CommandName);
        // Example: Block poop command during certain conditions
        // You could check player permissions, game state, etc.
        if (args.CommandName.Equals("poopcolor", StringComparison.OrdinalIgnoreCase))
        {
            // Example: Block the "poopcolor" command and notify via chat
            args.Cancel = true;
            PrintToChat(args.Player, "The poop color command is currently disabled.");
            return;
        }
        // Uncomment to block:
        // args.Cancel = true;

        // Example: Allow by default
        args.Cancel = false;
    }

    #endregion
}
