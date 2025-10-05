namespace Prefix.Poop.Models;

/// <summary>
/// Configuration for a sound with optional volume override
/// </summary>
internal sealed class SoundConfig
{
    /// <summary>
    /// Sound event name (e.g., "poop.poop_sound_01")
    /// </summary>
    public string SoundEvent { get; set; } = string.Empty;

    /// <summary>
    /// Optional volume override for this specific sound (0.0 to 1.0)
    /// If null, uses the global sound volume setting
    /// </summary>
    public float? Volume { get; set; }
}
