using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Interfaces.Modules.PoopModule;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Models;
using Prefix.Poop.Shared;
using Prefix.Poop.Shared.Models;
using Prefix.Poop.Utils;
using Prefix.Poop.Extensions;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.PoopModule.Lifecycle;
using Sharp.Shared.Enums;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Module handling all poop-related console commands
/// </summary>
internal sealed class PoopCommands : IModule
{
    private readonly ILogger<PoopCommands> _logger;
    private readonly InterfaceBridge _bridge;
    private readonly ICommandManager _commandManager;
    private readonly IConfigManager _config;
    private readonly ILocaleManager _locale;
    private readonly IPoopDatabase _database;
    private readonly IPoopSpawner _spawner;
    private readonly IPoopSizeGenerator _sizeGenerator;
    private readonly IPoopLifecycleManager _lifecycleManager;
    private readonly IPoopColorMenu _colorMenu;
    private readonly IPoopPlayerManager _poopPlayerManager;
    private readonly IPoopShared _sharedInterface;
    private readonly CommandCooldownTracker _cooldowns;

    public PoopCommands(
        ILogger<PoopCommands> logger,
        InterfaceBridge bridge,
        ICommandManager commandManager,
        IConfigManager config,
        ILocaleManager locale,
        IPoopDatabase database,
        IPoopSpawner spawner,
        IPoopSizeGenerator sizeGenerator,
        IPoopLifecycleManager lifecycleManager,
        IPoopColorMenu colorMenu,
        IPoopPlayerManager poopPlayerManager,
        IPoopShared sharedInterface)
    {
        _logger = logger;
        _bridge = bridge;
        _commandManager = commandManager;
        _config = config;
        _locale = locale;
        _database = database;
        _spawner = spawner;
        _sizeGenerator = sizeGenerator;
        _lifecycleManager = lifecycleManager;
        _colorMenu = colorMenu;
        _poopPlayerManager = poopPlayerManager;
        _sharedInterface = sharedInterface;
        _cooldowns = new CommandCooldownTracker(_config.CommandCooldownSeconds);
        
        // Set up per-command cooldowns
        _cooldowns.SetCommandCooldown("poop", _config.PoopCommand.CooldownSeconds);
        _cooldowns.SetCommandCooldown("poopcolor", _config.ColorCommand.CooldownSeconds);
        _cooldowns.SetCommandCooldown("toppoopers", _config.TopPoopersCommand.CooldownSeconds);
        _cooldowns.SetCommandCooldown("toppoop", _config.TopVictimsCommand.CooldownSeconds);
    }

    public bool Init()
    {
        // Initialize the chat prefix for extension methods
        Format.InitializeChatPrefix(_config.ChatPrefix);
        RegisterCommands();

        return true;
    }


    private void RegisterCommands()
    {
        // Register all command groups using helper method
        RegisterCommandGroup(_config.PoopCommand, OnPoopCommand, "Poop spawn");
        RegisterCommandGroup(_config.ColorCommand, OnPoopColorCommand, "Color menu");
        RegisterCommandGroup(_config.TopPoopersCommand, OnTopPoopersCommand, "Top poopers");
        RegisterCommandGroup(_config.TopVictimsCommand, OnTopVictimsCommand, "Top victims");

        // Debug/admin commands (server console only)
        _commandManager.AddServerCommand("poop_dryrun", OnPoopDryrunCommand);
    }

    #region Command Handlers - Using CommandManager

    /// <summary>
    /// Command: !toppoopers, !pooperstop
    /// Shows top poopers (players who placed the most poops)
    /// </summary>
    private ECommandAction OnTopPoopersCommand(IGamePlayer player, StringCommand command)
    {
        if (!ShouldAllowCommand(player, "toppoopers"))
        {
            return ECommandAction.Handled;
        }

        // Fire-and-forget pattern
        OnTopPoopersCommandAsync(player);
        return ECommandAction.Handled;
    }

    /// <summary>
    /// Async implementation of toppoopers command
    /// </summary>
    private void OnTopPoopersCommandAsync(IGamePlayer player)
    {
        // 1. Get player controller and validate (on main thread)
        var controller = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
        if (controller == null)
        {
            _logger.LogWarning("Could not find controller for player {player} (slot {slot})", player.Name, player.Slot);
            return;
        }

        // 2. Check cooldown (on main thread)

        if (!_cooldowns.CanExecute("toppoopers", player.Client))
        {
            var remaining = _cooldowns.GetRemainingCooldown("toppoopers", player.Client);
            controller.PrintToChat(_locale.GetString("common.cooldown", new Dictionary<string, object>
            {
                ["remaining"] = remaining
            }));
            return;
        }

        // 3. Run database query on background thread
        Task.Run(async () =>
        {
            try
            {
                var topPoopers = await _database.GetTopPoopersAsync(_config.TopRecordsLimit);

                // 4. Marshal results back to main thread for display
                await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                {
                    // Re-get controller in case player disconnected
                    var ctrl = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
                    if (ctrl == null) return;

                    if (topPoopers.Length == 0)
                    {
                        ctrl.PrintToChat(_locale.GetString("leaderboard.no_poopers"));
                        return;
                    }

                    ctrl.PrintToChat(_locale.GetString("leaderboard.top_poopers_title", new Dictionary<string, object>
                    {
                        ["count"] = topPoopers.Length
                    }));
                    
                    for (int i = 0; i < topPoopers.Length; i++)
                    {
                        var record = topPoopers[i];
                        ctrl.PrintToChat(_locale.GetString("leaderboard.top_poopers_entry", new Dictionary<string, object>
                        {
                            ["rank"] = i + 1,
                            ["playerName"] = record.Name,
                            ["poopCount"] = record.PoopCount
                        }));
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing toppoopers command for {player}", player.Name);
                await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                {
                    var ctrl = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
                    ctrl?.PrintToChat(_locale.GetString("leaderboard.error_poopers"));
                });
            }
        });
    }

    /// <summary>
    /// Command: !toppoop, !pooptop, !pooplist
    /// Shows top poop victims (players who were pooped on the most)
    /// </summary>
    private ECommandAction OnTopVictimsCommand(IGamePlayer player, StringCommand command)
    {
        if (!ShouldAllowCommand(player, "toppoop"))
        {
            return ECommandAction.Handled;
        }

        // Fire-and-forget pattern
        OnTopVictimsCommandAsync(player);
        return ECommandAction.Handled;
    }

    /// <summary>
    /// Async implementation of top victims command
    /// </summary>
    private void OnTopVictimsCommandAsync(IGamePlayer player)
    {
        // 1. Get player controller and validate (on main thread)
        var controller = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
        if (controller == null)
        {
            _logger.LogWarning("Could not find controller for player {player} (slot {slot})", player.Name, player.Slot);
            return;
        }

        if (!_cooldowns.CanExecute("toppoop", player.Client))
        {
            var remaining = _cooldowns.GetRemainingCooldown("toppoop", player.Client);
            controller.PrintToChat(_locale.GetString("common.cooldown", new Dictionary<string, object>
            {
                ["remaining"] = remaining
            }));
            return;
        }

        // 3. Run database query on background thread
        Task.Run(async () =>
        {
            try
            {
                var topVictims = await _database.GetTopVictimsAsync(_config.TopRecordsLimit);

                // 4. Marshal results back to main thread for display
                await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                {
                    // Re-get controller in case player disconnected
                    var ctrl = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
                    if (ctrl == null) return;

                    if (topVictims.Length == 0)
                    {
                        ctrl.PrintToChat(_locale.GetString("leaderboard.no_victims"));
                        return;
                    }

                    ctrl.PrintToChat(_locale.GetString("leaderboard.top_victims_title", new Dictionary<string, object>
                    {
                        ["count"] = topVictims.Length
                    }));
                    
                    for (int i = 0; i < topVictims.Length; i++)
                    {
                        var record = topVictims[i];
                        ctrl.PrintToChat(_locale.GetString("leaderboard.top_victims_entry", new Dictionary<string, object>
                        {
                            ["rank"] = i + 1,
                            ["playerName"] = record.Name,
                            ["poopCount"] = record.VictimCount
                        }));
                    }

                    _logger.LogDebug("Displayed top {count} victims to {player}", topVictims.Length, player.Name);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing top victims command for {player}", player.Name);
                await _bridge.ModSharp.InvokeFrameActionAsync(() =>
                {
                    var ctrl = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
                    ctrl?.PrintToChat(_locale.GetString("leaderboard.error_victims"));
                });
            }
        });
    }

    /// <summary>
    /// Command: !poopcolor, !poop_color, !colorpoop
    /// Opens color selection menu for the player
    /// </summary>
    private ECommandAction OnPoopColorCommand(IGamePlayer player, StringCommand command)
    {
        if (!ShouldAllowCommand(player, "poopcolor"))
        {
            return ECommandAction.Handled;
        }

        OpenColorMenuForPlayer(player);
        return ECommandAction.Handled;
    }

    /// <summary>
    /// Opens the color menu for a player
    /// </summary>
    private void OpenColorMenuForPlayer(IGamePlayer player)
    {
        try
        {
            _colorMenu.OpenColorMenu(player);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening color menu for {player}", player.Name);
            var controller = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
            controller?.PrintToChat(_locale.GetString("color.menu_error"));
        }
    }

    /// <summary>
    /// Command: !poop, !shit
    /// Spawns a poop on nearest dead player
    /// </summary>
    private ECommandAction OnPoopCommand(IGamePlayer player, StringCommand command)
    {
        if (!ShouldAllowCommand(player, "poop"))
        {
            return ECommandAction.Handled;
        }

        SpawnPoopForPlayer(player);
        return ECommandAction.Handled;
    }

    /// <summary>
    /// Spawns a poop for the player
    /// </summary>
    private void SpawnPoopForPlayer(IGamePlayer player)
    {
        try
        {
            // 1. Get player controller and validate
            var controller = _bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
            if (controller == null)
            {
                _logger.LogWarning("Could not find controller for player {player} (slot {slot})", player.Name, player.Slot);
                return;
            }

            var pawn = controller.GetPlayerPawn();
            if (pawn == null || !pawn.IsValid())
            {
                controller.PrintToChat(_locale.GetString("poop.must_be_alive"));
                return;
            }

            if (!_cooldowns.CanExecute("poop", player.Client))
            {
                var remaining = _cooldowns.GetRemainingCooldown("poop", player.Client);
                controller.PrintToChat(_locale.GetString("common.cooldown", new Dictionary<string, object>
                {
                    ["remaining"] = remaining
                }));
                return;
            }

            // 3. Check max poops per round limit
            if (_lifecycleManager.HasReachedMaxPoopsPerRound())
            {
                controller.PrintToChat(_locale.GetString("poop.max_per_round"));
                return;
            }

            // 4. Check if player is on the ground
            if (!pawn.Flags.HasFlag(EntityFlags.OnGround))
            {
                controller.PrintToChat(_locale.GetString("poop.must_be_on_ground"));
                return;
            }

            // 5. Get player position
            var position = pawn.GetAbsOrigin();

            // 6. Find nearest dead player (victim can be null)
            var victim = _spawner.FindNearestDeadPlayer(player.Client)?.Player;

            // 7. Get player's color preference from PoopPlayerManager (handles cache)
            // Check if color preferences are enabled
            if (_config.EnableColorPreferences)
            {
                // Get color preference from cache (preloaded on connect)
                var colorPref = _poopPlayerManager.GetColorPreference(player.Client.SteamId);

                // If random mode, get a random color each time
                if (colorPref.IsRandom)
                {
                    colorPref = ColorUtils.GetRandomColor(_config.AvailableColors);
                }

                _spawner.SpawnPoopWithFullLogic(
                    player.Client,
                    size: -1.0f,
                    colorPref,
                    playSounds: true,
                    showMessages: true);
            }
            else
            {
                // Use default color when preferences are disabled
                var (r, g, b) = _config.GetDefaultColorRgb();
                var colorPref = new PoopColorPreference(r, g, b);
                _spawner.SpawnPoopWithFullLogic(
                    player.Client,
                    size: -1.0f,
                    colorPref,
                    playSounds: true,
                    showMessages: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnPoopCommand for {player}", player.Name);
            player.Client.ConsolePrint($"{_config.ChatPrefix} An error occurred while spawning poop.");
        }
    }

    /// <summary>
    /// Command: poop_dryrun (server console only)
    /// Simulates poop size generation for testing
    /// </summary>
    private ECommandAction OnPoopDryrunCommand(StringCommand command)
    {
        _logger.LogInformation("Server executed poop_dryrun with args: {args}", command.ArgString);

        try
        {
            // 1. Parse count argument (default: 1000)
            int sampleCount = 1000;
            if (!string.IsNullOrWhiteSpace(command.ArgString))
            {
                if (!int.TryParse(command.ArgString.Trim(), out sampleCount) || sampleCount <= 0)
                {
                    _logger.LogInformation("Invalid count. Please provide a positive number.");
                    return ECommandAction.Handled;
                }

                // Cap at reasonable maximum
                if (sampleCount > 100000)
                {
                    _logger.LogInformation("Count limited to 100,000 to prevent performance issues.");
                    sampleCount = 100000;
                }
            }

            _logger.LogInformation($"Simulating {sampleCount} poop generations...");

            // 2. Track statistics
            float minSize = float.MaxValue;
            float maxSize = float.MinValue;
            float totalSize = 0;

            // Track distribution in specific ranges
            Dictionary<string, int> sizeCategories = new Dictionary<string, int>
            {
                {"Microscopic (< 0.5)", 0},
                {"Tiny (0.5 - 0.7)", 0},
                {"Small (0.7 - 0.9)", 0},
                {"Normal (0.9 - 1.1)", 0},
                {"Above Average (1.1 - 1.4)", 0},
                {"Large (1.4 - 1.7)", 0},
                {"Huge (1.7 - 2.0)", 0},
                {"MASSIVE (2.0 - 2.5)", 0},
                {"LEGENDARY (≥ 2.5)", 0}
            };

            // Count legendary sizes individually
            Dictionary<float, int> legendaryExactSizes = new Dictionary<float, int>();

            // 3. Simulate many generations
            for (int i = 0; i < sampleCount; i++)
            {
                float size = _sizeGenerator.GetRandomSize();

                // Update statistics
                minSize = Math.Min(minSize, size);
                maxSize = Math.Max(maxSize, size);
                totalSize += size;

                // Update distribution counts
                if (size < 0.5f) sizeCategories["Microscopic (< 0.5)"]++;
                else if (size < 0.7f) sizeCategories["Tiny (0.5 - 0.7)"]++;
                else if (size < 0.9f) sizeCategories["Small (0.7 - 0.9)"]++;
                else if (size < 1.1f) sizeCategories["Normal (0.9 - 1.1)"]++;
                else if (size < 1.4f) sizeCategories["Above Average (1.1 - 1.4)"]++;
                else if (size < 1.7f) sizeCategories["Large (1.4 - 1.7)"]++;
                else if (size < 2.0f) sizeCategories["Huge (1.7 - 2.0)"]++;
                else if (size < 2.5f) sizeCategories["MASSIVE (2.0 - 2.5)"]++;
                else
                {
                    sizeCategories["LEGENDARY (≥ 2.5)"]++;

                    // Track legendary sizes
                    float roundedSize = (float)Math.Round(size * 1000) / 1000;
                    legendaryExactSizes.TryAdd(roundedSize, 0);
                    legendaryExactSizes[roundedSize]++;
                }
            }

            // 4. Calculate average
            float avgSize = totalSize / sampleCount;

            // 5. Display results
            _logger.LogInformation($"=== Poop Size Distribution ({sampleCount} samples) ===");
            _logger.LogInformation($"Min: {minSize:F3}, Max: {maxSize:F3}, Avg: {avgSize:F3}");
            _logger.LogInformation("Size categories:");

            foreach (var category in sizeCategories)
            {
                float percentage = (float)category.Value / sampleCount * 100;
                _logger.LogInformation($"{category.Key}: {category.Value} ({percentage:F2}%)");
            }

            // Show legendary sizes if any exist
            if (legendaryExactSizes.Count > 0)
            {
                _logger.LogInformation("Legendary sizes found:");
                foreach (var legendary in legendaryExactSizes.OrderByDescending(x => x.Key))
                {
                    _logger.LogInformation($"Size {legendary.Key:F3}: {legendary.Value} times");
                }
            }

            _logger.LogInformation("=== End of Report ===");

            _logger.LogInformation("Completed poop_dryrun simulation with {count} samples", sampleCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in poop_dryrun command");
            _logger.LogInformation("Error running dryrun simulation.");
        }

        return ECommandAction.Handled;
    }

    #endregion
    /// <summary>
    /// Checks if a command should be allowed to execute by firing the shared interface event.
    /// Returns true if the command should proceed, false if it was cancelled.
    /// </summary>
    private bool ShouldAllowCommand(IGamePlayer player, string commandName)
    {
        if (_sharedInterface is SharedInterface.SharedInterface sharedInterface)
        {
            if (!sharedInterface.FirePoopCommand(player, commandName))
            {
                // Command was cancelled by event handler
                _logger.LogDebug("Command '{commandName}' was cancelled by event handler for player {player}", commandName, player.Name);
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Helper method to register a group of command aliases
    /// </summary>
    private int RegisterCommandGroup(
        CommandConfig config,
        ICommandManager.ClientCommandDelegate handler,
        string commandGroupName)
    {
        if (!config.Enabled)
        {
            _logger.LogInformation("{commandGroup} commands are disabled in configuration", commandGroupName);
            return 0;
        }

        foreach (var cmd in config.Aliases)
        {
            _commandManager.AddClientChatCommand(cmd, handler);
        }

        _logger.LogDebug("Registered {count} {commandGroup} command(s) with {cooldown}s cooldown",
            config.Aliases.Length, commandGroupName, config.CooldownSeconds);

        return config.Aliases.Length;
    }
}
