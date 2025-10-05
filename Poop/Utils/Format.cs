using System.Collections.Generic;
using Sharp.Shared.Definition;

namespace Prefix.Poop.Utils;

/// <summary>
/// Utility class for formatting chat messages with color codes and prefixes
/// </summary>
public static class Format
{
    private static string? _chatPrefix;
    private static readonly Dictionary<string, string> ColorCache = new(System.StringComparer.OrdinalIgnoreCase)
    {
        { "{white}", ChatColor.White },
        { "{default}", ChatColor.White },
        { "{darkred}", ChatColor.DarkRed },
        { "{pink}", ChatColor.Pink },
        { "{green}", ChatColor.Green },
        { "{lightgreen}", ChatColor.LightGreen },
        { "{lime}", ChatColor.Lime },
        { "{red}", ChatColor.Red },
        { "{grey}", ChatColor.Grey },
        { "{gray}", ChatColor.Grey },
        { "{yellow}", ChatColor.Yellow },
        { "{gold}", ChatColor.Gold },
        { "{silver}", ChatColor.Silver },
        { "{blue}", ChatColor.Blue },
        { "{lightblue}", ChatColor.Blue },
        { "{darkblue}", ChatColor.DarkBlue },
        { "{purple}", ChatColor.Purple },
        { "{lightred}", ChatColor.LightRed },
        { "{muted}", ChatColor.Muted },
        { "{head}", ChatColor.Head }
    };

    /// <summary>
    /// Initialize the chat prefix (call this once during plugin initialization)
    /// </summary>
    public static void InitializeChatPrefix(string prefix)
    {
        _chatPrefix = prefix;
    }

    /// <summary>
    /// Format a chat message with the configured prefix and color code support
    /// </summary>
    public static string ChatMessage(string message)
    {
        var processedMessage = ProcessColorCodes(message);
        return string.IsNullOrEmpty(_chatPrefix)
            ? processedMessage
            : $"{ProcessColorCodes(_chatPrefix)} {processedMessage}";
    }

    /// <summary>
    /// Format a console message with the configured prefix (no color codes)
    /// Strips any color codes from the message since console doesn't support them
    /// </summary>
    public static string ConsoleMessage(string message)
    {
        // Strip color codes from the message
        var cleanMessage = StripColorCodes(message);
        
        return string.IsNullOrEmpty(_chatPrefix)
            ? cleanMessage
            : $"{StripColorCodes(_chatPrefix)} {cleanMessage}";
    }

    /// <summary>
    /// Replace color placeholders like {red}, {blue}, etc. with actual ChatColor codes
    /// Uses a fast string replacement approach instead of regex
    /// </summary>
    public static string ProcessColorCodes(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        // Quick check if message even contains color codes
        if (!message.Contains('{'))
            return message;

        var result = message;
        foreach (var kvp in ColorCache)
        {
            // Case-insensitive search and replace
            if (result.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
            {
                result = result.Replace(kvp.Key, kvp.Value, System.StringComparison.OrdinalIgnoreCase);
            }
        }

        return result;
    }

    /// <summary>
    /// Strip color codes and placeholders from a message
    /// Removes both actual ChatColor codes and placeholders like {red}, {blue}, etc.
    /// </summary>
    public static string StripColorCodes(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var result = message;

        // First, remove color placeholders like {red}, {blue}, etc.
        if (result.Contains('{'))
        {
            foreach (var kvp in ColorCache)
            {
                if (result.Contains(kvp.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Replace(kvp.Key, string.Empty, System.StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        // Then, remove actual ChatColor codes (they start with \x01-\x10)
        // ChatColor values are special characters that won't display well in console
        foreach (var kvp in ColorCache)
        {
            if (result.Contains(kvp.Value))
            {
                result = result.Replace(kvp.Value, string.Empty);
            }
        }

        return result;
    }
}
