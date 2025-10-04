using Prefix.Poop.Interfaces;

namespace Prefix.Poop.Interfaces.Managers;

/// <summary>
/// Configuration manager interface for accessing plugin and module settings
/// </summary>
internal interface IConfigManager : IManager
{
    /// <summary>
    /// Check if a SteamID is an admin
    /// </summary>
    bool IsAdmin(string steamId);

    /// <summary>
    /// Path to the poop model file
    /// </summary>
    string PoopModel { get; }

    /// <summary>
    /// Minimum poop size
    /// </summary>
    float MinPoopSize { get; }

    /// <summary>
    /// Maximum poop size
    /// </summary>
    float MaxPoopSize { get; }

    /// <summary>
    /// Default poop size
    /// </summary>
    float DefaultPoopSize { get; }

    /// <summary>
    /// Chance percentage for common size poops
    /// </summary>
    int CommonSizeChance { get; }

    /// <summary>
    /// Chance percentage for small size poops
    /// </summary>
    int SmallSizeChance { get; }

    /// <summary>
    /// Chance for rare/large size
    /// </summary>
    int RareSizeChance { get; }

    /// <summary>
    /// Number of records to show in leaderboards
    /// </summary>
    int TopRecordsLimit { get; }

    /// <summary>
    /// Command cooldown in seconds
    /// </summary>
    int CommandCooldownSeconds { get; }

    /// <summary>
    /// Maximum distance to find dead players
    /// </summary>
    float MaxDeadPlayerDistance { get; }

    /// <summary>
    /// Use ragdoll entity detection for finding dead players
    /// </summary>
    bool UseRagdollVictimDetection { get; }

    /// <summary>
    /// Maximum distance for ragdoll detection
    /// </summary>
    float RagdollDetectionDistance { get; }

    /// <summary>
    /// Show chat message when poop is placed
    /// </summary>
    bool ShowMessageOnPoop { get; }

    /// <summary>
    /// Enable rainbow poop animation
    /// </summary>
    bool EnableRainbowPoops { get; }

    /// <summary>
    /// Rainbow animation speed
    /// </summary>
    float RainbowAnimationSpeed { get; }

    /// <summary>
    /// Database connection string
    /// </summary>
    string DatabaseConnection { get; }

    /// <summary>
    /// Database host
    /// </summary>
    string DatabaseHost { get; }

    /// <summary>
    /// Database port
    /// </summary>
    int DatabasePort { get; }

    /// <summary>
    /// Database name
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Database user
    /// </summary>
    string DatabaseUser { get; }

    /// <summary>
    /// Database password
    /// </summary>
    string DatabasePassword { get; }

    /// <summary>
    /// Enable database auto-migration
    /// </summary>
    bool DatabaseAutoMigrate { get; }

    /// <summary>
    /// Sound effect volume
    /// </summary>
    float SoundVolume { get; }

    /// <summary>
    /// List of poop sound effect names
    /// </summary>
    string[] PoopSounds { get; }

    /// <summary>
    /// Enable sound effects
    /// </summary>
    bool EnableSounds { get; }

    /// <summary>
    /// Enable player color preferences
    /// </summary>
    bool EnableColorPreferences { get; }

    /// <summary>
    /// Chat prefix for poop messages
    /// </summary>
    string ChatPrefix { get; }

    /// <summary>
    /// Enable debug logging
    /// </summary>
    bool DebugMode { get; }

    /// <summary>
    /// Maximum poops per round
    /// </summary>
    int MaxPoopsPerRound { get; }

    /// <summary>
    /// Remove poops on round end
    /// </summary>
    bool RemovePoopsOnRoundEnd { get; }

    /// <summary>
    /// Poop lifetime in seconds
    /// </summary>
    int PoopLifetimeSeconds { get; }

    /// <summary>
    /// Path to sound events file
    /// </summary>
    string SoundEventsFile { get; }

    /// <summary>
    /// Gets the default poop color as RGB tuple
    /// </summary>
    (int R, int G, int B) GetDefaultColorRgb();
}
