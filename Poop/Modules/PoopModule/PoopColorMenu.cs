using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Extensions;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
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
    // Available color options with their RGB values
    private readonly Dictionary<string, PoopColorPreference> _availableColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Brown (Default)", new PoopColorPreference(139, 69, 19) },
        { "White", new PoopColorPreference(255, 255, 255) },
        { "Black", new PoopColorPreference(0, 0, 0) },
        { "Red", new PoopColorPreference(255, 0, 0) },
        { "Green", new PoopColorPreference(0, 255, 0) },
        { "Blue", new PoopColorPreference(0, 0, 255) },
        { "Yellow", new PoopColorPreference(255, 255, 0) },
        { "Purple", new PoopColorPreference(128, 0, 128) },
        { "Orange", new PoopColorPreference(255, 165, 0) },
        { "Pink", new PoopColorPreference(255, 105, 180) },
        { "Cyan", new PoopColorPreference(0, 255, 255) },
        { "Gold", new PoopColorPreference(255, 215, 0) },
        { "Lime", new PoopColorPreference(0, 255, 0) },
        { "Magenta", new PoopColorPreference(255, 0, 255) },
        { "Silver", new PoopColorPreference(192, 192, 192) },
        { "Rainbow 🌈", new PoopColorPreference(255, 0, 0, true) }, // Rainbow mode
        { "Random 🎲", new PoopColorPreference(0, 0, 0, false, true) } // Random mode
    };

    // Random number generator for random color selection
    private readonly Random _random = new();

    /// <summary>
    /// Opens the color selection menu for a player
    /// </summary>
    public async Task OpenColorMenuAsync(IGamePlayer player)
    {
        // Check if color preferences are enabled
        if (!config.EnableColorPreferences)
        {
            var controller = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
            controller?.PrintToChat(locale.GetString("color.disabled"));
            logger.LogDebug("Player {player} tried to open color menu but EnableColorPreferences is disabled", player.Name);
            return;
        }

        var playerController = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
        if (playerController == null)
        {
            logger.LogWarning("Could not find controller for player {player}", player.Name);
            return;
        }

        // Parse and validate SteamID

        // Load the player's current color preference
        var currentPref = await poopPlayerManager.GetColorPreferenceAsync(player.Client.SteamId);

        var menu = new CenterHtmlMenu("Poop Color Selection", menuManager)
        {
            ShowKeyNumbers = true
        };

        // Add color options
        foreach (var colorEntry in _availableColors)
        {
            var colorName = colorEntry.Key;
            var colorPref = colorEntry.Value;

            // Skip rainbow option if rainbow poops are disabled
            if (colorPref.IsRainbow && !config.EnableRainbowPoops)
            {
                continue;
            }

            // Check if this is the currently selected color
            bool isSelected = IsColorSelected(currentPref, colorPref);

            menu.AddMenuOption(
                isSelected ? $"{colorName} ✓" : colorName,
                (p, _) =>
                {
                    // Fire-and-forget async call with proper error handling
                    Task.Run(async () => await OnColorSelected(p, p.Client.SteamId, colorName, colorPref));
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
        string colorName,
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
                var colorStr = GetColorCode(colorPref);
                controller.PrintToChat(locale.GetString("color.set_normal", new Dictionary<string, object>
                {
                    ["colorCode"] = colorStr,
                    ["colorName"] = colorName
                }));
            }

            logger.LogInformation($"Player {player.Name} ({steamId}) changed poop color to {colorName}");
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
    /// Gets a random color from the available colors (excluding Rainbow and Random)
    /// </summary>
    public PoopColorPreference GetRandomColor()
    {
        // Filter out Rainbow and Random options
        var normalColors = _availableColors.Values
            .Where(c => c is { IsRainbow: false, IsRandom: false })
            .ToList();

        if (normalColors.Count == 0)
        {
            // Fallback to brown if somehow no colors available
            return new PoopColorPreference(139, 69, 19);
        }

        var randomIndex = _random.Next(normalColors.Count);
        var randomColor = normalColors[randomIndex];

        logger.LogDebug("Selected random color: RGB({r}, {g}, {b})",
            randomColor.Red, randomColor.Green, randomColor.Blue);

        return randomColor;
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

    /// <summary>
    /// Gets a chat color code for preview (approximation)
    /// </summary>
    private static string GetColorCode(PoopColorPreference pref)
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

    public bool Init()
    {
        return true;
    }
}