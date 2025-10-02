using System;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Configuration settings for the Poop module
/// Loaded from appsettings.json
/// </summary>
public sealed class PoopModuleConfig
{
    /// <summary>
    /// Path to the poop model file
    /// Default: "models/yappershq/fun/poop.vmdl"
    /// </summary>
    public string PoopModel { get; set; } = "models/yappershq/fun/poop.vmdl";

    /// <summary>
    /// Minimum poop size (for rare small poops)
    /// Default: 0.3
    /// Range: 0.1 - 1.0
    /// </summary>
    public float MinPoopSize { get; set; } = 0.3f;

    /// <summary>
    /// Maximum poop size (for rare large poops)
    /// Default: 2.0
    /// Range: 1.0 - 5.0
    /// </summary>
    public float MaxPoopSize { get; set; } = 2.0f;

    /// <summary>
    /// Default poop size (for common poops)
    /// Default: 1.0
    /// Range: 0.5 - 2.0
    /// </summary>
    public float DefaultPoopSize { get; set; } = 1.0f;

    /// <summary>
    /// Chance percentage for common size poops
    /// Default: 85
    /// Range: 0 - 100
    /// </summary>
    public int CommonSizeChance { get; set; } = 85;

    /// <summary>
    /// Chance percentage for small size poops
    /// Default: 10
    /// Range: 0 - 100
    /// </summary>
    public int SmallSizeChance { get; set; } = 10;

    /// <summary>
    /// Chance for rare/large size calculated as: 100 - CommonSizeChance - SmallSizeChance
    /// </summary>
    public int RareSizeChance => 100 - CommonSizeChance - SmallSizeChance;

    /// <summary>
    /// Number of records to show in leaderboards
    /// Default: 10
    /// Range: 1 - 100
    /// </summary>
    public int TopRecordsLimit { get; set; } = 10;

    /// <summary>
    /// Command cooldown in seconds
    /// Default: 3
    /// Range: 1 - 300
    /// </summary>
    public int CommandCooldownSeconds { get; set; } = 3;

    /// <summary>
    /// Maximum distance to find dead players
    /// Default: 500.0 units
    /// Range: 50 - 5000
    /// </summary>
    public float MaxDeadPlayerDistance { get; set; } = 500.0f;

    /// <summary>
    /// Show chat message when poop is placed
    /// Default: true
    /// </summary>
    public bool ShowMessageOnPoop { get; set; } = true;

    /// <summary>
    /// Enable rainbow poop animation
    /// Default: true
    /// </summary>
    public bool EnableRainbowPoops { get; set; } = true;

    /// <summary>
    /// Rainbow animation speed (hue change per tick)
    /// Default: 2.0
    /// Range: 0.1 - 10.0
    /// </summary>
    public float RainbowAnimationSpeed { get; set; } = 2.0f;

    /// <summary>
    /// Database connection type (mysql, postgresql, etc.)
    /// Default: "mysql"
    /// </summary>
    public string DatabaseType { get; set; } = "mysql";

    /// <summary>
    /// Database connection string (optional, can be built from individual settings)
    /// Default: ""
    /// </summary>
    public string DatabaseConnection { get; set; } = string.Empty;

    /// <summary>
    /// Database host (for MySQL/PostgreSQL)
    /// Default: "localhost"
    /// </summary>
    public string DatabaseHost { get; set; } = "localhost";

    /// <summary>
    /// Database port (for MySQL/PostgreSQL)
    /// Default: 3306 (MySQL)
    /// </summary>
    public int DatabasePort { get; set; } = 3306;

    /// <summary>
    /// Database name
    /// Default: "poopdb"
    /// </summary>
    public string DatabaseName { get; set; } = "poopdb";

    /// <summary>
    /// Database user
    /// Default: "root"
    /// </summary>
    public string DatabaseUser { get; set; } = "root";

    /// <summary>
    /// Database password
    /// Default: ""
    /// </summary>
    public string DatabasePassword { get; set; } = string.Empty;

    /// <summary>
    /// Enable database auto-migration on startup
    /// Default: true
    /// </summary>
    public bool DatabaseAutoMigrate { get; set; } = true;

    /// <summary>
    /// Sound effect volume (0.0 - 1.0)
    /// Default: 0.5
    /// </summary>
    public float SoundVolume { get; set; } = 0.5f;

    /// <summary>
    /// List of poop sound effect names
    /// </summary>
    public string[] PoopSounds { get; set; } = new[]
    {
        "poop.poop_sound_01",
        "poop.poop_sound_02",
        "poop.poop_sound_03"
    };

    /// <summary>
    /// Enable sound effects when poop is placed
    /// Default: true
    /// </summary>
    public bool EnableSounds { get; set; } = true;

    /// <summary>
    /// Default poop color (RGB format: "R,G,B")
    /// Default: "139,69,19" (brown)
    /// </summary>
    public string DefaultPoopColor { get; set; } = "139,69,19";

    /// <summary>
    /// Enable player color preferences
    /// Default: true
    /// </summary>
    public bool EnableColorPreferences { get; set; } = true;

    /// <summary>
    /// Chat prefix for poop messages
    /// Default: " [Poop]"
    /// </summary>
    public string ChatPrefix { get; set; } = " [Poop]";

    /// <summary>
    /// Enable debug logging
    /// Default: false
    /// </summary>
    public bool DebugMode { get; set; } = false;

    /// <summary>
    /// Maximum number of poops per round
    /// Default: 0 (unlimited)
    /// Range: 0 - 1000
    /// </summary>
    public int MaxPoopsPerRound { get; set; }

    /// <summary>
    /// Remove poops on round end
    /// Default: false
    /// </summary>
    public bool RemovePoopsOnRoundEnd { get; set; } = false;

    /// <summary>
    /// Poop lifetime in seconds (0 = permanent until round end)
    /// Default: 0
    /// Range: 0 - 600
    /// </summary>
    public int PoopLifetimeSeconds { get; set; }

    /// <summary>
    /// Validates the configuration and logs warnings for invalid values
    /// </summary>
    public void Validate()
    {
        // Clamp size values
        MinPoopSize = Math.Clamp(MinPoopSize, 0.1f, 1.0f);
        MaxPoopSize = Math.Clamp(MaxPoopSize, 1.0f, 5.0f);
        DefaultPoopSize = Math.Clamp(DefaultPoopSize, 0.5f, 2.0f);

        // Ensure min < default < max
        if (MinPoopSize >= DefaultPoopSize)
            MinPoopSize = DefaultPoopSize * 0.5f;
        
        if (MaxPoopSize <= DefaultPoopSize)
            MaxPoopSize = DefaultPoopSize * 2.0f;

        // Clamp chance values
        CommonSizeChance = Math.Clamp(CommonSizeChance, 0, 100);
        SmallSizeChance = Math.Clamp(SmallSizeChance, 0, 100);

        // Ensure chances don't exceed 100%
        if (CommonSizeChance + SmallSizeChance > 100)
        {
            SmallSizeChance = 100 - CommonSizeChance;
        }

        // Clamp other values
        TopRecordsLimit = Math.Clamp(TopRecordsLimit, 1, 100);
        CommandCooldownSeconds = Math.Clamp(CommandCooldownSeconds, 1, 300);
        MaxDeadPlayerDistance = Math.Clamp(MaxDeadPlayerDistance, 50.0f, 5000.0f);
        RainbowAnimationSpeed = Math.Clamp(RainbowAnimationSpeed, 0.1f, 10.0f);
        SoundVolume = Math.Clamp(SoundVolume, 0.0f, 1.0f);
        MaxPoopsPerRound = Math.Clamp(MaxPoopsPerRound, 0, 1000);
        PoopLifetimeSeconds = Math.Clamp(PoopLifetimeSeconds, 0, 600);
    }

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
}
