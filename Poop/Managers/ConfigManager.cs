using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;

namespace Prefix.Poop.Managers;

/// <summary>
/// Centralized configuration manager for the Poop plugin
/// Handles both global and module-specific configuration
/// </summary>
internal sealed class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly HashSet<string> _adminSteamIds;

    // Global Configuration
    public IReadOnlySet<string> AdminSteamIds => _adminSteamIds;

    // Module Configuration - Poop Settings
    public string PoopModel { get; }
    public string SoundEventsFile { get; }
    public float MinPoopSize { get; }
    public float MaxPoopSize { get; }
    public float DefaultPoopSize { get; }
    public int CommonSizeChance { get; }
    public int SmallSizeChance { get; }
    public int RareSizeChance => 100 - CommonSizeChance - SmallSizeChance;
    public int TopRecordsLimit { get; }
    public int CommandCooldownSeconds { get; }
    public float MaxDeadPlayerDistance { get; }
    public bool UseRagdollVictimDetection { get; }
    public float RagdollDetectionDistance { get; }
    public bool ShowMessageOnPoop { get; }
    public int MaxPoopsPerRound { get; }
    public bool RemovePoopsOnRoundEnd { get; }
    public int PoopLifetimeSeconds { get; }

    // Color Settings
    public bool EnableRainbowPoops { get; }
    public float RainbowAnimationSpeed { get; }
    public string DefaultPoopColor { get; }
    public bool EnableColorPreferences { get; }

    // Sound Settings
    public bool EnableSounds { get; }
    public float SoundVolume { get; }
    public string[] PoopSounds { get; }

    // Database Settings
    public string DatabaseConnection { get; }
    public bool DatabaseAutoMigrate { get; }
    public string DatabaseHost { get; }
    public int DatabasePort { get; }
    public string DatabaseName { get; }
    public string DatabaseUser { get; }
    public string DatabasePassword { get; }

    // UI Settings
    public string ChatPrefix { get; }
    public bool DebugMode { get; }

    public ConfigManager(ILogger<ConfigManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Load global admin list
        var adminList = configuration.GetSection("AdminSteamIds").Get<List<string>>() ?? new List<string>();
        _adminSteamIds = new HashSet<string>(adminList);

        // Load PoopModule configuration
        var poopModule = configuration.GetSection("PoopModule");

        PoopModel = poopModule.GetValue("PoopModel", "models/yappershq/fun/poop.vmdl");
        SoundEventsFile = poopModule.GetValue("SoundEventsFile", "soundevents/poop_sounds.vsndevts");

        MinPoopSize = poopModule.GetValue("MinPoopSize", 0.3f);
        MaxPoopSize = poopModule.GetValue("MaxPoopSize", 2.0f);
        DefaultPoopSize = poopModule.GetValue("DefaultPoopSize", 1.0f);
        CommonSizeChance = poopModule.GetValue("CommonSizeChance", 85);
        SmallSizeChance = poopModule.GetValue("SmallSizeChance", 10);

        TopRecordsLimit = poopModule.GetValue("TopRecordsLimit", 10);
        CommandCooldownSeconds = poopModule.GetValue("CommandCooldownSeconds", 3);
        MaxDeadPlayerDistance = poopModule.GetValue("MaxDeadPlayerDistance", 500.0f);
        UseRagdollVictimDetection = poopModule.GetValue("UseRagdollVictimDetection", true);
        RagdollDetectionDistance = poopModule.GetValue("RagdollDetectionDistance", 100.0f);
        ShowMessageOnPoop = poopModule.GetValue("ShowMessageOnPoop", true);
        MaxPoopsPerRound = poopModule.GetValue("MaxPoopsPerRound", 0);
        RemovePoopsOnRoundEnd = poopModule.GetValue("RemovePoopsOnRoundEnd", false);
        PoopLifetimeSeconds = poopModule.GetValue("PoopLifetimeSeconds", 0);

        EnableRainbowPoops = poopModule.GetValue("EnableRainbowPoops", true);
        RainbowAnimationSpeed = poopModule.GetValue("RainbowAnimationSpeed", 2.0f);
        DefaultPoopColor = poopModule.GetValue("DefaultPoopColor", "139,69,19");
        EnableColorPreferences = poopModule.GetValue("EnableColorPreferences", true);

        EnableSounds = poopModule.GetValue("EnableSounds", true);
        SoundVolume = poopModule.GetValue("SoundVolume", 0.5f);
        PoopSounds = poopModule.GetSection("PoopSounds").Get<string[]>() ?? new[]
        {
            "poop.poop_sound_01",
            "poop.poop_sound_02",
            "poop.poop_sound_03"
        };

        DatabaseConnection = poopModule.GetValue("DatabaseConnection", string.Empty);
        DatabaseAutoMigrate = poopModule.GetValue("DatabaseAutoMigrate", true);
        DatabaseHost = poopModule.GetValue("DatabaseHost", "localhost");
        DatabasePort = poopModule.GetValue("DatabasePort", 3306);
        DatabaseName = poopModule.GetValue("DatabaseName", "poopdb");
        DatabaseUser = poopModule.GetValue("DatabaseUser", "root");
        DatabasePassword = poopModule.GetValue("DatabasePassword", string.Empty);

        ChatPrefix = poopModule.GetValue("ChatPrefix", " [Poop]");
        DebugMode = poopModule.GetValue("DebugMode", false);

        Validate();

        _logger.LogInformation("ConfigManager initialized with {adminCount} admins", _adminSteamIds.Count);
    }

    /// <summary>
    /// Check if a SteamID is an admin
    /// </summary>
    public bool IsAdmin(string steamId) => _adminSteamIds.Contains(steamId);

    /// <summary>
    /// Gets the default poop color as RGB tuple
    /// </summary>
    public (int R, int G, int B) GetDefaultColorRgb()
    {
        var parts = DefaultPoopColor.Split(',');
        if (parts.Length == 3 &&
            int.TryParse(parts[0], out var r) &&
            int.TryParse(parts[1], out var g) &&
            int.TryParse(parts[2], out var b))
        {
            return (Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
        }

        return (139, 69, 19); // Default brown
    }

    /// <summary>
    /// Validates the configuration and clamps values to acceptable ranges
    /// </summary>
    private void Validate()
    {
        // Size validation happens at runtime since properties are readonly
        if (MinPoopSize < 0.1f || MinPoopSize > 1.0f)
            _logger.LogWarning("MinPoopSize {value} is outside recommended range (0.1 - 1.0)", MinPoopSize);

        if (MaxPoopSize < 1.0f || MaxPoopSize > 5.0f)
            _logger.LogWarning("MaxPoopSize {value} is outside recommended range (1.0 - 5.0)", MaxPoopSize);

        if (DefaultPoopSize < 0.5f || DefaultPoopSize > 2.0f)
            _logger.LogWarning("DefaultPoopSize {value} is outside recommended range (0.5 - 2.0)", DefaultPoopSize);

        if (MinPoopSize >= DefaultPoopSize)
            _logger.LogWarning("MinPoopSize ({min}) should be less than DefaultPoopSize ({default})",
                MinPoopSize, DefaultPoopSize);

        if (MaxPoopSize <= DefaultPoopSize)
            _logger.LogWarning("MaxPoopSize ({max}) should be greater than DefaultPoopSize ({default})",
                MaxPoopSize, DefaultPoopSize);

        if (CommonSizeChance + SmallSizeChance > 100)
            _logger.LogWarning("CommonSizeChance ({common}) + SmallSizeChance ({small}) exceeds 100%",
                CommonSizeChance, SmallSizeChance);

        if (RareSizeChance < 0)
            _logger.LogWarning("RareSizeChance is negative ({rare}%), adjust CommonSizeChance or SmallSizeChance",
                RareSizeChance);
    }

    public bool Init()
    {
        return true;
    }
}
