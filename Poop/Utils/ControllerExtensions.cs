using System.Collections.Generic;
using Sharp.Shared.Definition;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Utils;

/// <summary>
/// Extension methods for IPlayerController to simplify chat/console printing
/// </summary>
public static class ControllerExtensions
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
    /// Print a message to all players' chat with the configured prefix and color code support
    /// </summary>
    public static string FormatChatMessage(string message)
    {
        var processedMessage = ProcessColorCodes(message);
        return string.IsNullOrEmpty(_chatPrefix)
            ? processedMessage
            : $"{ProcessColorCodes(_chatPrefix)} {processedMessage}";
    }

    /// <summary>
    /// Replace color placeholders like {red}, {blue}, etc. with actual ChatColor codes
    /// Uses a fast string replacement approach instead of regex
    /// </summary>
    private static string ProcessColorCodes(string message)
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
    /// Print a message to the player's chat with the configured prefix and color code support
    /// </summary>
    public static void PrintToChat(this IPlayerController controller, string message)
    {
        var processedMessage = ProcessColorCodes(message);
        var fullMessage = string.IsNullOrEmpty(_chatPrefix)
            ? processedMessage
            : $"{ProcessColorCodes(_chatPrefix)} {processedMessage}";

        controller.Print(HudPrintChannel.Chat, fullMessage);
    }

    /// <summary>
    /// Print a message to the player's console with the configured prefix
    /// </summary>
    public static void PrintToConsole(this IPlayerController controller, string message)
    {
        var fullMessage = string.IsNullOrEmpty(_chatPrefix)
            ? message
            : $"{_chatPrefix} {message}";

        controller.Print(HudPrintChannel.Console, fullMessage);
    }

    /// <summary>
    /// Print a message to the player's center screen (HTML-like formatting)
    /// </summary>
    public static void PrintToCenter(this IPlayerController controller, string message)
    {
        controller.Print(HudPrintChannel.Hint, message);
    }

    /// <summary>
    /// Print a message to the player's hint area (HTML-like formatting)
    /// </summary>
    public static void PrintToHint(this IPlayerController controller, string message)
    {
        controller.Print(HudPrintChannel.Hint, message);
    }


}
