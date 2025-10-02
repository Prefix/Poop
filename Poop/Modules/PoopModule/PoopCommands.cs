using Prefix.Poop.Interfaces;
using Microsoft.Extensions.Logging;
using Sharp.Shared;
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
    private readonly PoopModuleConfig _config;
    private readonly IPoopDatabase _database;
    private readonly CommandCooldownTracker _cooldowns;

    public PoopCommands(
        ILogger<PoopCommands> logger,
        InterfaceBridge bridge,
        PoopModuleConfig config,
        IPoopDatabase database)
    {
        _logger = logger;
        _bridge = bridge;
        _config = config;
        _database = database;
        _cooldowns = new CommandCooldownTracker(_config.CommandCooldownSeconds);

        // Register console commands
        RegisterCommands();
    }

    public bool Init()
    {
        _logger.LogInformation("PoopCommands initialized");
        _logger.LogInformation("Cooldown: {cooldown}s, Top Records: {records}, Max Distance: {distance}",
            _config.CommandCooldownSeconds, _config.TopRecordsLimit, _config.MaxDeadPlayerDistance);
        return true;
    }

    public void OnPostInit()
    {
        _logger.LogInformation("PoopCommands post-initialized");
    }

    public void Shutdown()
    {
        _logger.LogInformation("PoopCommands shutting down");
    }

    private void RegisterCommands()
    {
        _logger.LogInformation("Registering poop commands...");

        // Register client commands using ClientManager.InstallCommandCallback
        // ModSharp uses command names without prefix (ms_ is added by chat trigger)
        
        // Statistics commands
        _bridge.ClientManager.InstallCommandCallback("toppoopers", OnTopPoopersCommand);
        _bridge.ClientManager.InstallCommandCallback("pooperstop", OnTopPoopersCommand);
        
        _bridge.ClientManager.InstallCommandCallback("toppoop", OnTopVictimsCommand);
        _bridge.ClientManager.InstallCommandCallback("pooptop", OnTopVictimsCommand);
        _bridge.ClientManager.InstallCommandCallback("pooplist", OnTopVictimsCommand);
        
        // Main poop commands
        _bridge.ClientManager.InstallCommandCallback("poop", OnPoopCommand);
        _bridge.ClientManager.InstallCommandCallback("shit", OnPoopCommand);
        
        // Size control commands
        _bridge.ClientManager.InstallCommandCallback("poop_size", OnPoopSizeCommand);
        _bridge.ClientManager.InstallCommandCallback("poop_rnd", OnPoopRandomCommand);
        
        // Debug/admin commands (server console only)
        _bridge.ConVarManager.CreateServerCommand("ms_poop_dryrun", OnPoopDryrunCommand, "Simulate poop size distribution", ConVarFlags.Release);
        
        _logger.LogInformation("Registered 11 poop commands");
    }

    #region Command Handlers - ModSharp Style

    /// <summary>
    /// Command: ms_toppoopers, ms_pooperstop
    /// Shows top poopers (players who placed the most poops)
    /// </summary>
    private ECommandAction OnTopPoopersCommand(IGameClient client, StringCommand command)
    {
        _logger.LogInformation("{player} executed {cmd}", client.Name, command.GetCommandString());

        // TODO: Implementation
        // 1. Check cooldown: _cooldowns.CanExecute("toppoopers", client.SteamID64)
        // 2. Query database: await _database.GetTopPoopersAsync(_config.TopRecordsLimit)
        // 3. Send results to client via _bridge.ModSharp.PrintChannelFilter(...)

        client.ConsolePrint($"{_config.ChatPrefix} Top Poopers feature coming soon!");
        
        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: ms_toppoop, ms_pooptop, ms_pooplist
    /// Shows top poop victims (players who were pooped on the most)
    /// </summary>
    private ECommandAction OnTopVictimsCommand(IGameClient client, StringCommand command)
    {
        _logger.LogInformation("{player} executed {cmd}", client.Name, command.GetCommandString());

        // TODO: Implementation
        // 1. Check cooldown
        // 2. Query database: await _database.GetTopVictimsAsync(_config.TopRecordsLimit)
        // 3. Send results to client

        client.ConsolePrint($"{_config.ChatPrefix} Top Victims feature coming soon!");
        
        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: ms_poop, ms_shit
    /// Spawns a poop on nearest dead player
    /// </summary>
    private ECommandAction OnPoopCommand(IGameClient client, StringCommand command)
    {
        _logger.LogInformation("{player} executed {cmd}", client.Name, command.GetCommandString());

        // TODO: Implementation
        // 1. Validate player is alive and on ground
        // 2. Check cooldown
        // 3. Find nearest dead player within _config.MaxDeadPlayerDistance
        // 4. Generate poop size using _config rarity system
        // 5. Spawn poop entity at dead player location
        // 6. Play sound, show message
        // 7. Save to database

        client.ConsolePrint($"{_config.ChatPrefix} Poop feature coming soon!");
        
        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: ms_poop_size
    /// Spawns a poop with specific size
    /// </summary>
    private ECommandAction OnPoopSizeCommand(IGameClient client, StringCommand command)
    {
        _logger.LogInformation("{player} executed {cmd}", client.Name, command.GetCommandString());

        // TODO: Implementation
        // Parse size from command.GetArg(1)
        // Clamp between MinPoopSize and MaxPoopSize
        // Spawn with exact size

        client.ConsolePrint($"{_config.ChatPrefix} Poop Size feature coming soon!");
        
        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: ms_poop_rnd
    /// Spawns a poop with completely random size
    /// </summary>
    private ECommandAction OnPoopRandomCommand(IGameClient client, StringCommand command)
    {
        _logger.LogInformation("{player} executed {cmd}", client.Name, command.GetCommandString());

        // TODO: Implementation
        // Generate random size between MinPoopSize and MaxPoopSize
        // Spawn poop

        client.ConsolePrint($"{_config.ChatPrefix} Random Poop feature coming soon!");
        
        return ECommandAction.Stopped;
    }

    /// <summary>
    /// Command: ms_poop_dryrun (server console only)
    /// Simulates poop size generation for testing
    /// </summary>
    private ECommandAction OnPoopDryrunCommand(StringCommand command)
    {
        _logger.LogInformation("Server executed {cmd}", command.GetCommandString());

        // TODO: Implementation
        // Parse count from command.GetArg(1)
        // Simulate N poop generations
        // Display distribution statistics

        _bridge.ModSharp.LogMessage("Poop Dryrun feature coming soon!");
        
        return ECommandAction.Stopped;
    }

    #endregion

    #region Command Handlers - Statistics

    /// <summary>
    /// Command: css_toppoopers, css_pooperstop
    /// Shows top poopers (players who placed the most poops)
    /// Client only command
    /// </summary>
    /// <remarks>
    /// Implementation:
    /// 1. Check command cooldown (3 seconds)
    /// 2. Query database for top poopers
    /// 3. Display results in chat
    /// 4. Handle async database calls properly
    /// </remarks>
    private void OnTopPoopers(/* player, args */)
    {
        _logger.LogInformation("Command: css_toppoopers");

        // TODO: Implementation
        // - Validate player is valid
        // - Check command cooldown: _cooldowns.CanExecute("toppoopers", steamId)
        // - Query database: await _database.GetTopPoopersAsync(_config.TopRecordsLimit)
        // - Display results in chat with formatting using _config.ChatPrefix:
        //   "{_config.ChatPrefix} === Top X Poopers ==="
        //   "{_config.ChatPrefix} #1: PlayerName - 999 poops"
        // - Handle database errors gracefully
    }

    /// <summary>
    /// Command: css_toppoop, css_pooptop, css_pooplist
    /// Shows top poop victims (players who were pooped on the most)
    /// Client only command
    /// </summary>
    /// <remarks>
    /// Implementation:
    /// 1. Check command cooldown (3 seconds)
    /// 2. Query database for top victims
    /// 3. Display results in chat
    /// </remarks>
    private void OnTopVictims(/* player, args */)
    {
        _logger.LogInformation("Command: css_toppoop");

        // TODO: Implementation
        // - Validate player is valid
        // - Check command cooldown: _cooldowns.CanExecute("toppoop", steamId)
        // - Query database: await _database.GetTopVictimsAsync(_config.TopRecordsLimit)
        // - Display results in chat with formatting using _config.ChatPrefix:
        //   "{_config.ChatPrefix} === Top X Poop Victims ==="
        //   "{_config.ChatPrefix} #1: PlayerName - 999 times"
        // - Handle database errors gracefully
    }

    #endregion

    #region Command Handlers - Poop Placement

    /// <summary>
    /// Command: css_poop, css_shit
    /// Spawns a poop on the nearest dead player
    /// Client only command
    /// </summary>
    /// <remarks>
    /// Implementation:
    /// 1. Check command cooldown (3 seconds)
    /// 2. Validate player is alive
    /// 3. Find nearest dead player position
    /// 4. Generate random poop size based on rarity
    /// 5. Spawn poop entity at dead player position
    /// 6. Update database with poop count
    /// 7. Display message to chat
    /// </remarks>
    private void OnPoop(/* player, args */)
    {
        _logger.LogInformation("Command: css_poop");

        // TODO: Implementation
        // - Validate player is valid and alive
        // - Check command cooldown: _cooldowns.CanExecute("poop", steamId)
        // - Find nearest dead player position within _config.MaxDeadPlayerDistance
        // - If no dead player found, notify player
        // - Generate random poop size using _config values:
        //   * Common (_config.CommonSizeChance%): _config.DefaultPoopSize (0.5-1.5)
        //   * Small (_config.SmallSizeChance%): _config.MinPoopSize (0.1-0.5)
        //   * Rare (remaining%): _config.MaxPoopSize (1.5-3.0)
        // - Apply player's color preference (or _config.GetDefaultColorRgb())
        // - Spawn poop prop using _config.PoopModel
        // - If _config.EnableSounds, play random sound from _config.PoopSounds at _config.SoundVolume
        // - Update database: await _database.IncrementPoopCountAsync(...) and IncrementVictimCountAsync(...)
        // - If _config.ShowMessageOnPoop, display: "{_config.ChatPrefix} PlayerName pooped on DeadPlayerName! (Size: X.XX)"
        // - If _config.EnableRainbowPoops and player has rainbow enabled, start animation
    }

    /// <summary>
    /// Command: css_poop_size
    /// Spawns a poop with a specific size
    /// Client only command
    /// Usage: css_poop_size [size]
    /// </summary>
    /// <remarks>
    /// Implementation:
    /// 1. Parse size argument
    /// 2. Validate size is within bounds (MinPoopSize to MaxPoopSize)
    /// 3. Spawn poop with exact size
    /// </remarks>
    private void OnPoopSize(/* player, args */)
    {
        _logger.LogInformation("Command: css_poop_size");

        // TODO: Implementation
        // - Validate player is valid and alive
        // - Check command cooldown: _cooldowns.CanExecute("poop_size", steamId)
        // - Parse size argument from command
        // - Validate size: _config.MinPoopSize <= size <= _config.MaxPoopSize
        // - If invalid size, notify player of valid range
        // - Find nearest dead player position within _config.MaxDeadPlayerDistance
        // - Spawn poop with exact specified size using _config.PoopModel
        // - Apply player's color preference (or _config.GetDefaultColorRgb())
        // - Update database
        // - If _config.ShowMessageOnPoop, display message with exact size
    }

    /// <summary>
    /// Command: css_poop_rnd
    /// Spawns a poop with a completely random size
    /// Client only command
    /// </summary>
    /// <remarks>
    /// Implementation:
    /// 1. Generate random size between MinPoopSize and MaxPoopSize
    /// 2. Spawn poop with random size
    /// </remarks>
    private void OnPoopRandom(/* player, args */)
    {
        _logger.LogInformation("Command: css_poop_rnd");

        // TODO: Implementation
        // - Validate player is valid and alive
        // - Check command cooldown: _cooldowns.CanExecute("poop_rnd", steamId)
        // - Generate completely random size: _config.MinPoopSize to _config.MaxPoopSize
        // - Find nearest dead player position within _config.MaxDeadPlayerDistance
        // - Spawn poop with random size using _config.PoopModel
        // - Apply player's color preference (or _config.GetDefaultColorRgb())
        // - Update database
        // - If _config.ShowMessageOnPoop, display: "{_config.ChatPrefix} PlayerName pooped on DeadPlayerName! (Random Size: X.XX)"
    }

    #endregion

    #region Command Handlers - Debug/Admin

    /// <summary>
    /// Command: css_poop_dryrun
    /// Simulates generating many poops to analyze size distribution
    /// Server only command (admin/testing)
    /// Usage: css_poop_dryrun [count]
    /// </summary>
    /// <remarks>
    /// Implementation:
    /// 1. Parse count argument (default: 1000)
    /// 2. Simulate size generation N times
    /// 3. Display distribution statistics
    /// </remarks>
    private void OnPoopDryRun(/* player, args */)
    {
        _logger.LogInformation("Command: css_poop_dryrun");

        // TODO: Implementation
        // - Validate caller (server console or admin)
        // - Parse count argument (default: 1000)
        // - Run simulation using _config values:
        //   * Generate N poop sizes using the rarity algorithm with _config settings
        //   * Count occurrences in each category (common, small, rare)
        // - Calculate statistics:
        //   * Average size
        //   * Min/Max size generated
        //   * Percentage in each category
        // - Display results:
        //   "{_config.ChatPrefix} === Poop Size Distribution (N samples) ==="
        //   "{_config.ChatPrefix} Common ({_config.CommonSizeChance}%): Y occurrences (avg size: Z)"
        //   "{_config.ChatPrefix} Small ({_config.SmallSizeChance}%): Y occurrences (avg size: Z)"
        //   "{_config.ChatPrefix} Rare ({_config.RareSizeChance}%): Y occurrences (avg size: Z)"
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Checks if player can execute command based on cooldown
    /// </summary>
    private static bool CheckCommandCooldown(/* player, commandName */)
    {
        // TODO: Implementation
        // - Extract player's SteamID
        // - Use _cooldowns.CanExecute(commandName, steamId)
        // - If false, get remaining time: _cooldowns.GetRemainingCooldown(commandName, steamId)
        // - Notify player: "{_config.ChatPrefix} Please wait {remaining:F1}s before using this command again."
        // - Return result
        return true;
    }

    /// <summary>
    /// Finds the nearest dead player position to the given player
    /// </summary>
    private static void FindNearestDeadPlayer(/* player */)
    {
        // TODO: Implementation
        // - Get player's current position
        // - Iterate through deadPlayers dictionary
        // - Calculate distance to each dead player
        // - Filter by _config.MaxDeadPlayerDistance
        // - Return closest dead player position and name
        // - Return null if no dead players available within range
    }

    /// <summary>
    /// Spawns a poop entity at the specified position
    /// </summary>
    private static void SpawnPoop(/* position, size, color, victimName */)
    {
        // TODO: Implementation
        // - Create prop_dynamic_override entity
        // - Set model to _config.PoopModel
        // - Set position and rotation
        // - Set scale based on size parameter
        // - Apply color (RGB values)
        // - Set collision group
        // - Spawn entity
        // - If _config.EnableRainbowPoops, add to rainbow poop tracking
        // - If _config.PoopLifetimeSeconds > 0, schedule removal timer
        // - If _config.EnableSounds, play random sound from _config.PoopSounds at _config.SoundVolume
    }

    /// <summary>
    /// Generates a random poop size based on rarity configuration
    /// </summary>
    private float GenerateRandomPoopSize()
    {
        // TODO: Implementation
        // - Roll random percentage (0-100)
        // - If <= _config.CommonSizeChance: return random between (_config.DefaultPoopSize * 0.5f) and (_config.DefaultPoopSize * 1.5f)
        // - Else if <= _config.CommonSizeChance + _config.SmallSizeChance: return random between _config.MinPoopSize and (_config.MinPoopSize * 1.5f)
        // - Else: return random between (_config.MaxPoopSize * 0.75f) and _config.MaxPoopSize (rare)
        return _config.DefaultPoopSize;
    }

    #endregion
}
