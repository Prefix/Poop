using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Models;
using Prefix.Poop.Modules.PoopModule;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Units;

namespace Prefix.Poop.Managers;

/// <summary>
/// Centralized configuration manager for the Poop plugin
/// Handles both global and module-specific configuration
/// </summary>
internal sealed class ConfigManager : IConfigManager
{
    private readonly ILogger<ConfigManager> _logger;
    private readonly HashSet<string> _adminSteamIds;

    // Global Configuration
    public IReadOnlySet<string> AdminSteamIds => _adminSteamIds;

    // Module Configuration - Poop Settings
    public string PoopModel { get; }
    public string SoundEventsFile { get; }
    public float MinPoopSize { get; }
    public float MaxPoopSize { get; }
    public float DefaultPoopSize { get; }
    public List<PoopSizeGenerationTier> GenerationTiers { get; }
    public List<PoopSizeCategory> SizeCategories { get; }
    public float MassiveAnnouncementThreshold { get; }
    public int TopRecordsLimit { get; }
    public int CommandCooldownSeconds { get; }
    public CommandConfig PoopCommand { get; }
    public CommandConfig ColorCommand { get; }
    public CommandConfig TopPoopersCommand { get; }
    public CommandConfig TopVictimsCommand { get; }
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
    public Dictionary<string, PoopColorPreference> AvailableColors { get; }

    // Sound Settings
    public bool EnableSounds { get; }
    public float SoundVolume { get; }
    public SoundConfig[] PoopSoundsConfig { get; }
    public bool EnableTauntSounds { get; }
    public SoundConfig[] TauntSoundsConfig { get; }

    // Database Settings
    public string DatabaseConnection { get; }
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

        // Load global admin list
        var adminList = configuration.GetSection("AdminSteamIds").Get<List<string>>() ?? new List<string>();
        _adminSteamIds = new HashSet<string>(adminList);

        // Load PoopModule configuration
        var poopModule = configuration.GetSection("PoopModule");

        // Assets
        var assets = poopModule.GetSection("Assets");
        PoopModel = assets.GetValue("PoopModel", "models/yappershq/fun/poop.vmdl");
        SoundEventsFile = assets.GetValue("SoundEventsFile", "soundevents/poop_sounds.vsndevts");

        // Size configuration
        var sizeConfig = poopModule.GetSection("Size");
        MinPoopSize = sizeConfig.GetValue("MinPoopSize", 0.3f);
        MaxPoopSize = sizeConfig.GetValue("MaxPoopSize", 2.6f);
        DefaultPoopSize = sizeConfig.GetValue("DefaultPoopSize", 1.0f);
        MassiveAnnouncementThreshold = sizeConfig.GetValue("MassiveAnnouncementThreshold", 2.0f);

        // Load generation tiers with fallback to defaults
        // Note: The "Rare" tier (2%) uses sub-tiers for sophisticated weighted distribution:
        // Massive (80%), Legendary (19%), Ultra Legendary (1%). SubTiers can be used on any tier!
        GenerationTiers = sizeConfig.GetSection("GenerationTiers").Get<List<PoopSizeGenerationTier>>() ?? new()
        {
            new() { Chance = 40, Name = "Normal", MinMultiplier = 0.9f, MaxMultiplier = 1.1f },
            new() { Chance = 25, Name = "Above Average", MinMultiplier = 1.1f, MaxMultiplier = 1.4f },
            new() { Chance = 15, Name = "Small", MinMultiplier = 0.7f, MaxMultiplier = 0.9f },
            new() { Chance = 10, Name = "Large", MinMultiplier = 1.4f, MaxMultiplier = 1.7f },
            new() { Chance = 5, Name = "Tiny", MinMultiplier = 0.5f, MaxMultiplier = 0.7f },
            new() { Chance = 3, Name = "Huge", MinMultiplier = 1.7f, MaxMultiplier = 2.0f },
            new() 
            { 
                Chance = 2, 
                Name = "Rare", 
                MinMultiplier = 2.0f, 
                MaxMultiplier = 2.6f,
                SubTiers =
                [
                    new() { Weight = 80, Name = "Massive", MinRangePercent = 0.0f, MaxRangePercent = 0.833f },
                    // Legendary: 19% of rare tier (0.38% overall) - 2.5 to 2.6 range (83.3-100% of parent range)
                    new() { Weight = 19, Name = "Legendary", MinRangePercent = 0.833f, MaxRangePercent = 1.0f },
                    // Ultra Legendary: 1% of rare tier (0.02% overall) - 2.59 to 2.599 range (98-99.9% of parent range)
                    new() { Weight = 1, Name = "Ultra Legendary", MinRangePercent = 0.98f, MaxRangePercent = 0.999f }
                ]
            }
        };

        // Load size categories with fallback to defaults
        SizeCategories = sizeConfig.GetSection("SizeCategories").Get<List<PoopSizeCategory>>() ??
        [
            new() { Threshold = 2.5f, LocaleKey = "size.legendary" },
            new() { Threshold = 2.0f, LocaleKey = "size.desc_massive" },
            new() { Threshold = 1.7f, LocaleKey = "size.desc_huge" },
            new() { Threshold = 1.4f, LocaleKey = "size.desc_large" },
            new() { Threshold = 1.1f, LocaleKey = "size.desc_above_average" },
            new() { Threshold = 0.9f, LocaleKey = "size.desc_normal" },
            new() { Threshold = 0.7f, LocaleKey = "size.desc_small" },
            new() { Threshold = 0.5f, LocaleKey = "size.desc_tiny" },
            new() { Threshold = 0.0f, LocaleKey = "size.desc_microscopic" }
        ];

        // Victim detection configuration
        var victimDetection = poopModule.GetSection("VictimDetection");
        MaxDeadPlayerDistance = victimDetection.GetValue("MaxDeadPlayerDistance", 500.0f);
        UseRagdollVictimDetection = victimDetection.GetValue("UseRagdollVictimDetection", true);
        RagdollDetectionDistance = victimDetection.GetValue("RagdollDetectionDistance", 100.0f);

        // Gameplay configuration
        var gameplay = poopModule.GetSection("Gameplay");
        ShowMessageOnPoop = gameplay.GetValue("ShowMessageOnPoop", true);
        MaxPoopsPerRound = gameplay.GetValue("MaxPoopsPerRound", 0);
        RemovePoopsOnRoundEnd = gameplay.GetValue("RemovePoopsOnRoundEnd", false);
        PoopLifetimeSeconds = gameplay.GetValue("PoopLifetimeSeconds", 0);

        // Commands configuration
        var commands = poopModule.GetSection("Commands");
        TopRecordsLimit = commands.GetValue("TopRecordsLimit", 10);
        CommandCooldownSeconds = commands.GetValue("CommandCooldownSeconds", 3);
        
        // Load individual command configurations
        PoopCommand = commands.GetSection("Poop").Get<CommandConfig>() ?? new CommandConfig
        {
            Enabled = true,
            Aliases = ["poop", "shit"],
            CooldownSeconds = 3
        };
        
        ColorCommand = commands.GetSection("Color").Get<CommandConfig>() ?? new CommandConfig
        {
            Enabled = true,
            Aliases = ["poopcolor", "poop_color", "colorpoop"],
            CooldownSeconds = 2
        };
        
        TopPoopersCommand = commands.GetSection("TopPoopers").Get<CommandConfig>() ?? new CommandConfig
        {
            Enabled = true,
            Aliases = ["toppoopers", "pooperstop"],
            CooldownSeconds = 5
        };
        
        TopVictimsCommand = commands.GetSection("TopVictims").Get<CommandConfig>() ?? new CommandConfig
        {
            Enabled = true,
            Aliases = ["toppoop", "pooptop"],
            CooldownSeconds = 5
        };

        // Color configuration
        var colorConfig = poopModule.GetSection("Color");
        EnableRainbowPoops = colorConfig.GetValue("EnableRainbowPoops", true);
        RainbowAnimationSpeed = colorConfig.GetValue("RainbowAnimationSpeed", 2.0f);
        DefaultPoopColor = colorConfig.GetValue("DefaultPoopColor", "139,69,19");
        EnableColorPreferences = colorConfig.GetValue("EnableColorPreferences", true);

        // Load available colors from config with fallback to defaults
        var colorDefinitions = colorConfig.GetSection("AvailableColors").Get<List<PoopColorDefinition>>() ?? GetDefaultColorDefinitions();
        AvailableColors = colorDefinitions.ToDictionary(
            c => c.LocaleKey,
            c => c.ToPreference(),
            StringComparer.OrdinalIgnoreCase
        );

        // Sound configuration
        var soundConfig = poopModule.GetSection("Sound");
        EnableSounds = soundConfig.GetValue("EnableSounds", true);
        SoundVolume = soundConfig.GetValue("SoundVolume", 0.5f);
        
        // Load poop sounds with volume overrides
        PoopSoundsConfig = soundConfig.GetSection("PoopSounds").Get<SoundConfig[]>() ?? 
        [
            new SoundConfig { SoundEvent = "poop.poop_sound_01", Volume = null },
            new SoundConfig { SoundEvent = "poop.poop_sound_02", Volume = null },
            new SoundConfig { SoundEvent = "poop.poop_sound_03", Volume = null }
        ];
        
        // Load taunt sounds with volume overrides
        EnableTauntSounds = soundConfig.GetValue("EnableTauntSounds", true);
        TauntSoundsConfig = soundConfig.GetSection("TauntSounds").Get<SoundConfig[]>() ?? 
        [
            new SoundConfig { SoundEvent = "poop.poop_taunt_01", Volume = null },
            new SoundConfig { SoundEvent = "poop.poop_taunt_02", Volume = null }
        ];

        // Database configuration
        var dbConfig = poopModule.GetSection("Database");
        DatabaseConnection = dbConfig.GetValue("ConnectionString", string.Empty);
        DatabaseHost = dbConfig.GetValue("Host", "localhost");
        DatabasePort = dbConfig.GetValue("Port", 3306);
        DatabaseName = dbConfig.GetValue("Name", "poopdb");
        DatabaseUser = dbConfig.GetValue("User", "root");
        DatabasePassword = dbConfig.GetValue("Password", string.Empty);

        // UI configuration
        var uiConfig = poopModule.GetSection("UI");
        ChatPrefix = uiConfig.GetValue("ChatPrefix", " [Poop]");
        DebugMode = uiConfig.GetValue("DebugMode", false);

        Validate();
    }

    /// <summary>
    /// Check if a SteamID is an admin
    /// </summary>
    public bool IsAdmin(SteamID steamId) => _adminSteamIds.Contains(steamId.ToString());

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

        // Validate size categories
        if (SizeCategories.Count == 0)
        {
            _logger.LogWarning("SizeCategories is empty, size descriptions may not work correctly");
        }
        else
        {
            // Check if categories are sorted descending (largest to smallest)
            for (int i = 0; i < SizeCategories.Count - 1; i++)
            {
                if (SizeCategories[i].Threshold < SizeCategories[i + 1].Threshold)
                {
                    _logger.LogWarning(
                        "SizeCategories should be sorted by Threshold descending (largest first). " +
                        "Category at index {index} ({threshold1}) is smaller than next ({threshold2})",
                        i, SizeCategories[i].Threshold, SizeCategories[i + 1].Threshold);
                    break;
                }
            }

            // Check for missing locale keys
            foreach (var category in SizeCategories)
            {
                if (string.IsNullOrWhiteSpace(category.LocaleKey))
                {
                    _logger.LogWarning("SizeCategory with threshold {threshold} has empty LocaleKey", 
                        category.Threshold);
                }
            }
        }

        if (MassiveAnnouncementThreshold < 0)
        {
            _logger.LogWarning("MassiveAnnouncementThreshold ({value}) should not be negative",
                MassiveAnnouncementThreshold);
        }

        // Validate generation tiers
        if (GenerationTiers.Count == 0)
        {
            _logger.LogWarning("GenerationTiers is empty, size generation may not work correctly");
        }
        else
        {
            // Check if chances sum to 100
            int totalChance = 0;
            foreach (var tier in GenerationTiers)
            {
                totalChance += tier.Chance;

                // Validate individual tier properties
                if (tier.Chance <= 0)
                {
                    _logger.LogWarning("GenerationTier '{name}' has invalid Chance ({chance}), must be > 0",
                        tier.Name, tier.Chance);
                }

                if (tier.MinMultiplier <= 0 || tier.MaxMultiplier <= 0)
                {
                    _logger.LogWarning("GenerationTier '{name}' has invalid multipliers (Min: {min}, Max: {max})",
                        tier.Name, tier.MinMultiplier, tier.MaxMultiplier);
                }

                if (tier.MinMultiplier >= tier.MaxMultiplier)
                {
                    _logger.LogWarning("GenerationTier '{name}' MinMultiplier ({min}) should be less than MaxMultiplier ({max})",
                        tier.Name, tier.MinMultiplier, tier.MaxMultiplier);
                }

                if (string.IsNullOrWhiteSpace(tier.Name))
                {
                    _logger.LogWarning("GenerationTier with Chance {chance}% has empty Name", tier.Chance);
                }

                // Validate sub-tiers if defined
                if (tier.SubTiers is { Count: > 0 })
                {
                    int totalWeight = 0;
                    foreach (var subTier in tier.SubTiers)
                    {
                        totalWeight += subTier.Weight;

                            if (subTier.Weight <= 0)
                            {
                                _logger.LogWarning("SubTier '{subName}' in tier '{tierName}' has invalid Weight ({weight})",
                                    subTier.Name, tier.Name, subTier.Weight);
                            }

                            if (subTier.MinRangePercent < 0 || subTier.MinRangePercent > 1 ||
                                subTier.MaxRangePercent < 0 || subTier.MaxRangePercent > 1)
                            {
                                _logger.LogWarning("SubTier '{subName}' in tier '{tierName}' has invalid range percents (Min: {min}, Max: {max})",
                                    subTier.Name, tier.Name, subTier.MinRangePercent, subTier.MaxRangePercent);
                            }

                        if (subTier.MinRangePercent >= subTier.MaxRangePercent)
                        {
                            _logger.LogWarning("SubTier '{subName}' in tier '{tierName}' MinRangePercent ({min}) should be less than MaxRangePercent ({max})",
                                subTier.Name, tier.Name, subTier.MinRangePercent, subTier.MaxRangePercent);
                        }
                    }

                    if (totalWeight == 0)
                    {
                        _logger.LogWarning("GenerationTier '{name}' SubTiers have zero total weight", tier.Name);
                    }
                }
            }

            if (totalChance != 100)
            {
                _logger.LogWarning("GenerationTiers total chance is {total}%, should sum to 100%", totalChance);
            }
        }
    }

    /// <summary>
    /// Provides default color definitions if not specified in config
    /// </summary>
    private static List<PoopColorDefinition> GetDefaultColorDefinitions()
    {
        return
        [
            new() { LocaleKey = "color.brown_default", Red = 139, Green = 69, Blue = 19, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.white", Red = 255, Green = 255, Blue = 255, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.black", Red = 0, Green = 0, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.red", Red = 255, Green = 0, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.green", Red = 0, Green = 255, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.blue", Red = 0, Green = 0, Blue = 255, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.yellow", Red = 255, Green = 255, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.purple", Red = 128, Green = 0, Blue = 128, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.orange", Red = 255, Green = 165, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.pink", Red = 255, Green = 105, Blue = 180, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.cyan", Red = 0, Green = 255, Blue = 255, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.gold", Red = 255, Green = 215, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.lime", Red = 0, Green = 255, Blue = 0, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.magenta", Red = 255, Green = 0, Blue = 255, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.silver", Red = 192, Green = 192, Blue = 192, IsRainbow = false, IsRandom = false },
            new() { LocaleKey = "color.rainbow", Red = 255, Green = 0, Blue = 0, IsRainbow = true, IsRandom = false },
            new() { LocaleKey = "color.random", Red = 0, Green = 0, Blue = 0, IsRainbow = false, IsRandom = true }
        ];
    }

    public bool Init()
    {
        return true;
    }
}
