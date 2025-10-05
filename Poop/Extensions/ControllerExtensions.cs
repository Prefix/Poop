using Prefix.Poop.Utils;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Extensions;

/// <summary>
/// Extension methods for IPlayerController to simplify chat/console printing
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Get the IGameClient associated with this player controller
    /// </summary>
    public static IGameClient? GetGameClient(this IPlayerController self)
    {
        return InterfaceBridge.Instance.ClientManager.GetGameClient(self.SteamId);
    }

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

    /// <summary>
    /// Print a message to the player's center HTML HUD using the survival respawn status overlay
    /// This is a cleaner alternative to the CenterHtmlMenu system
    /// </summary>
    public static void PrintToCenterHtml(this IPlayerController controller, string message, int duration = 5)
    {
        Managers.Event.EventManager.Instance.PrintToCenterHtml(controller, message, duration);
    }


}
