using Prefix.Poop.Shared.Models;

namespace Prefix.Poop.Models;

/// <summary>
/// Configuration model for defining available poop colors
/// </summary>
internal sealed class PoopColorDefinition
{
    /// <summary>
    /// Locale key for the color name (e.g., "color.brown_default", "color.rainbow")
    /// </summary>
    public string LocaleKey { get; set; } = string.Empty;

    /// <summary>
    /// Red component (0-255)
    /// </summary>
    public int Red { get; set; }

    /// <summary>
    /// Green component (0-255)
    /// </summary>
    public int Green { get; set; }

    /// <summary>
    /// Blue component (0-255)
    /// </summary>
    public int Blue { get; set; }

    /// <summary>
    /// Whether this is a rainbow color option
    /// </summary>
    public bool IsRainbow { get; set; }

    /// <summary>
    /// Whether this is a random color option
    /// </summary>
    public bool IsRandom { get; set; }

    /// <summary>
    /// Converts this definition to a PoopColorPreference instance
    /// </summary>
    public PoopColorPreference ToPreference()
    {
        return new PoopColorPreference(Red, Green, Blue, IsRainbow, IsRandom);
    }
}
