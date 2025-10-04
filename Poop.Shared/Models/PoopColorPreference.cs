using System;

namespace Prefix.Poop.Shared.Models;

/// <summary>
/// Represents a player's poop color preference
/// </summary>
public sealed class PoopColorPreference
{
    public int Red { get; set; } = 139;
    public int Green { get; set; } = 69;
    public int Blue { get; set; } = 19;
    public bool IsRainbow { get; set; }
    public bool IsRandom { get; set; }

    public PoopColorPreference()
    {
    }

    public PoopColorPreference(int red, int green, int blue, bool isRainbow = false, bool isRandom = false)
    {
        Red = Math.Clamp(red, 0, 255);
        Green = Math.Clamp(green, 0, 255);
        Blue = Math.Clamp(blue, 0, 255);
        IsRainbow = isRainbow;
        IsRandom = isRandom;
    }

    public string ToColorString()
        => $"rgb({Red}, {Green}, {Blue})";
}
