using System;
using System.Collections.Generic;
using System.Linq;
using Prefix.Poop.Shared.Models;

namespace Prefix.Poop.Utils;

/// <summary>
/// Utility methods for poop color operations
/// </summary>
internal static class ColorUtils
{
    private static readonly Random Random = new();

    /// <summary>
    /// Gets a random color from the standard color palette (excluding Rainbow and Random)
    /// </summary>
    public static PoopColorPreference GetRandomColor(Dictionary<string, PoopColorPreference> availableColors)
    {
        var normalColors = availableColors.Values
            .Where(c => c is { IsRainbow: false, IsRandom: false })
            .ToArray();

        if (normalColors.Length == 0)
        {
            return new PoopColorPreference(139, 69, 19); // Default brown
        }

        var randomIndex = Random.Next(normalColors.Length);
        return normalColors[randomIndex];
    }

    /// <summary>
    /// Gets a chat color code approximation for a color preference
    /// </summary>
    public static string GetChatColorCode(PoopColorPreference pref)
    {
        // Map RGB to closest chat color
        if (pref is { Red: > 200, Green: < 100, Blue: < 100 }) return "{red}";
        if (pref is { Red: < 100, Green: > 200, Blue: < 100 }) return "{green}";
        if (pref is { Red: < 100, Green: < 100, Blue: > 200 }) return "{blue}";
        if (pref is { Red: > 200, Green: > 200, Blue: < 100 }) return "{yellow}";
        if (pref is { Red: > 200, Green: < 100, Blue: > 200 }) return "{purple}";
        if (pref is { Red: > 200, Green: > 100, Blue: < 100 }) return "{orange}";
        if (pref is { Red: > 200, Green: > 200, Blue: > 200 }) return "{white}";
        if (pref is { Red: < 100, Green: < 100, Blue: < 100 }) return "{grey}";
        if (pref is { Red: > 100, Green: > 100, Blue: > 100 }) return "{lightgrey}";
        if (pref is { Red: 139, Green: 69, Blue: 19 }) return "{orange}"; // Brown

        return "{default}";
    }
}
