using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Prefix.Poop.Interfaces.Modules.PoopModule;
using Prefix.Poop.Interfaces.PoopModule;
using Prefix.Poop.Managers.Menu;
using Prefix.Poop.Shared.Models;
using Prefix.Poop.Utils;
using Sharp.Shared.Units;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Handles the poop color selection menu
/// </summary>
internal sealed class PoopColorMenu(
    ILogger<PoopColorMenu> logger,
    IMenuManager menuManager,
    IPoopPlayerManager poopPlayerManager,
    InterfaceBridge bridge,
    IConfigManager config,
    ILocaleManager locale)
    : IPoopColorMenu
{
    /// <summary>
    /// Opens the color selection menu for a player
    /// </summary>
    public void OpenColorMenu(IGamePlayer player)
    {
        // Check if color preferences are enabled
        if (!config.EnableColorPreferences)
        {
            var controller = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
            controller?.PrintToChat(locale.GetString("color.disabled"));
            return;
        }

        var playerController = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
        if (playerController == null)
        {
            return;
        }

        // Load the player's current color preference from cache (preloaded on connect)
        var currentPref = poopPlayerManager.GetColorPreference(player.Client.SteamId);

        var menu = new CenterHtmlMenu("Poop Color Selection", menuManager)
        {
            ShowKeyNumbers = true
        };

        // Add color options
        foreach (var colorEntry in config.AvailableColors)
        {
            var localeKey = colorEntry.Key;
            var colorPref = colorEntry.Value;

            // Skip rainbow option if rainbow poops are disabled
            if (colorPref.IsRainbow && !config.EnableRainbowPoops)
            {
                continue;
            }

            // Check if this is the currently selected color
            bool isSelected = IsColorSelected(currentPref, colorPref);

            // Get translated color name
            var colorName = locale.GetString(localeKey);

            menu.AddMenuOption(
                isSelected ? $"{colorName} ✓" : colorName,
                (p, _) =>
                {
                    // Fire-and-forget async call with proper error handling
                    Task.Run(async () => await OnColorSelected(p, p.Client.SteamId, localeKey, colorPref));
                },
                disabled: isSelected // Disable currently selected color (can't reselect)
            );
        }

        menuManager.OpenCenterHtmlMenu(player, menu);
    }

    /// <summary>
    /// Handles when a player selects a color
    /// </summary>
    private async Task OnColorSelected(
        IGamePlayer player,
        SteamID steamId,
        string localeKey,
        PoopColorPreference colorPref)
    {
        try
        {
            // Get player controller for chat messages
            var controller = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
            if (controller == null)
            {
                logger.LogWarning("Could not find controller for player {player}", player.Name);
                return;
            }

            // Save via PoopPlayerManager (handles database + cache)
            await poopPlayerManager.SaveColorPreferenceAsync(steamId, colorPref);

            // Send confirmation message
            if (colorPref.IsRainbow)
            {
                controller.PrintToChat(locale.GetString("color.set_rainbow"));
                controller.PrintToChat(locale.GetString("color.set_rainbow_info"));
            }
            else if (colorPref.IsRandom)
            {
                controller.PrintToChat(locale.GetString("color.set_random"));
                controller.PrintToChat(locale.GetString("color.set_random_info"));
            }
            else
            {
                var colorStr = ColorUtils.GetChatColorCode(colorPref);
                var colorName = locale.GetString(localeKey);
                controller.PrintToChat(locale.GetString("color.set_normal", new Dictionary<string, object>
                {
                    ["colorCode"] = colorStr,
                    ["colorName"] = colorName
                }));
            }

            logger.LogInformation($"Player {player.Name} ({steamId}) changed poop color to {localeKey}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Failed to save color preference for player {steamId}");
            var controller = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
            controller?.PrintToChat(locale.GetString("color.save_error"));
        }
    }

    /// <summary>
    /// Gets a player's color preference (delegates to PoopPlayerManager)
    /// </summary>
    public async Task<PoopColorPreference> GetPlayerColorPreferenceAsync(SteamID steamId)
    {
        return await poopPlayerManager.GetColorPreferenceAsync(steamId);
    }

    /// <summary>
    /// Checks if a color preference matches the current selection
    /// </summary>
    private static bool IsColorSelected(PoopColorPreference? current, PoopColorPreference option)
    {
        if (current == null)
            return false;

        // For rainbow mode, just check the flag
        if (option.IsRainbow)
            return current.IsRainbow;

        // For random mode, just check the flag
        if (option.IsRandom)
            return current.IsRandom;

        // For normal colors, compare RGB values
        return current is { IsRainbow: false, IsRandom: false, Red: var r, Green: var g, Blue: var b } &&
               r == option.Red &&
               g == option.Green &&
               b == option.Blue;
    }

    public bool Init()
    {
        return true;
    }
}