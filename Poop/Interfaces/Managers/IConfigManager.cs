using System.Collections.Generic;
using Prefix.Poop.Modules.PoopModule;
using Prefix.Poop.Shared.Models;
using Sharp.Shared.Units;

namespace Prefix.Poop.Interfaces.Managers;

/// <summary>
/// Configuration manager interface for accessing plugin and module settings
/// Organized into logical sections matching appsettings.json structure
/// </summary>
internal interface IConfigManager : IManager
{
    /// <summary>
    /// Check if a SteamID is an admin
    /// </summary>
    bool IsAdmin(SteamID steamId);

    // ===== ASSETS SECTION =====
    
    /// <summary>
    /// Path to the poop model file
    /// </summary>
    string PoopModel { get; }

    /// <summary>
    /// Path to sound events file
    /// </summary>
    string SoundEventsFile { get; }

    // ===== SIZE SECTION =====
    
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
    /// Threshold for massive poop announcements
    /// </summary>
    float MassiveAnnouncementThreshold { get; }

    /// <summary>
    /// Generation tiers for size randomization with sub-tier support
    /// </summary>
    List<PoopSizeGenerationTier> GenerationTiers { get; }

    /// <summary>
    /// Dynamic size categories with thresholds and locale keys
    /// </summary>
    List<PoopSizeCategory> SizeCategories { get; }

    // ===== COLOR SECTION =====
    
    /// <summary>
    /// Enable rainbow poop animation
    /// </summary>
    bool EnableRainbowPoops { get; }

    /// <summary>
    /// Rainbow animation speed multiplier
    /// </summary>
    float RainbowAnimationSpeed { get; }

    /// <summary>
    /// Enable player color preferences
    /// </summary>
    bool EnableColorPreferences { get; }

    /// <summary>
    /// Dictionary of available color options (name -> preference)
    /// </summary>
    Dictionary<string, PoopColorPreference> AvailableColors { get; }

    /// <summary>
    /// Gets the default poop color as RGB tuple
    /// </summary>
    (int R, int G, int B) GetDefaultColorRgb();

    // ===== SOUND SECTION =====
    
    /// <summary>
    /// Enable sound effects
    /// </summary>
    bool EnableSounds { get; }

    /// <summary>
    /// Sound effect volume (0.0 to 1.0)
    /// </summary>
    float SoundVolume { get; }

    /// <summary>
    /// List of poop sound effect names
    /// </summary>
    string[] PoopSounds { get; }

    // ===== VICTIM DETECTION SECTION =====
    
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

    // ===== GAMEPLAY SECTION =====
    
    /// <summary>
    /// Show chat message when poop is placed
    /// </summary>
    bool ShowMessageOnPoop { get; }

    /// <summary>
    /// Maximum poops per round (0 = unlimited)
    /// </summary>
    int MaxPoopsPerRound { get; }

    /// <summary>
    /// Remove poops on round end
    /// </summary>
    bool RemovePoopsOnRoundEnd { get; }

    /// <summary>
    /// Poop lifetime in seconds (0 = permanent)
    /// </summary>
    int PoopLifetimeSeconds { get; }

    // ===== COMMANDS SECTION =====
    
    /// <summary>
    /// Number of records to show in leaderboards
    /// </summary>
    int TopRecordsLimit { get; }

    /// <summary>
    /// Command cooldown in seconds
    /// </summary>
    int CommandCooldownSeconds { get; }

    // ===== DATABASE SECTION =====
    
    /// <summary>
    /// Database connection string (overrides individual settings)
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

    // ===== UI SECTION =====
    
    /// <summary>
    /// Chat prefix for poop messages
    /// </summary>
    string ChatPrefix { get; }

    /// <summary>
    /// Enable debug logging
    /// </summary>
    bool DebugMode { get; }
}
