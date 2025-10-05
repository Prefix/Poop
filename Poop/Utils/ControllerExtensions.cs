using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Utils;

/// <summary>
/// Extension methods for IPlayerController to simplify chat/console printing
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Print a message to the player's chat with the configured prefix and color code support
    /// </summary>
    public static void PrintToChat(this IPlayerController controller, string message)
    {
        var fullMessage = Format.ChatMessage(message);
        controller.Print(HudPrintChannel.Chat, fullMessage);
    }

    /// <summary>
    /// Print a message to the player's console with the configured prefix
    /// </summary>
    public static void PrintToConsole(this IPlayerController controller, string message)
    {
        var fullMessage = Format.ConsoleMessage(message);
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
