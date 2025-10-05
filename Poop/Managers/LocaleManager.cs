using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;

namespace Prefix.Poop.Managers;

/// <summary>
/// Manages localized strings and translations for the Poop plugin
/// </summary>
internal sealed class LocaleManager : ILocaleManager
{
    private readonly ILogger<LocaleManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _localeDirectory;
    private Dictionary<string, string> _localizedStrings;
    private readonly InterfaceBridge _bridge;

    public string CurrentLocale { get; }

    public LocaleManager(ILogger<LocaleManager> logger, IConfiguration configuration, InterfaceBridge bridge)
    {
        _logger = logger;
        _configuration = configuration;
        _bridge = bridge;

        // Get locale from configuration, default to "EN"
        CurrentLocale = _configuration.GetValue<string>("Locale") ?? "EN";

        // Set locale directory path (relative to plugin directory)
        _localeDirectory = Path.Combine(_bridge.DllPath, "locales");

        // Initialize dictionary
        _localizedStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        _logger.LogInformation("LocaleManager initializing with locale: {locale}", CurrentLocale);
    }

    public bool Init()
    {
        try
        {
            // Ensure locale directory exists
            if (!Directory.Exists(_localeDirectory))
            {
                Directory.CreateDirectory(_localeDirectory);
                _logger.LogWarning("Locale directory did not exist, created: {directory}", _localeDirectory);

                // Create default English locale file
                CreateDefaultEnglishLocale();
            }

            // Load locale file
            LoadLocale(CurrentLocale);

            _logger.LogInformation("LocaleManager initialized successfully with {count} strings", _localizedStrings.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize LocaleManager");
            return false;
        }
    }

    public void Shutdown()
    {
        _logger.LogInformation("LocaleManager shutting down");
        _localizedStrings.Clear();
    }

    public string GetString(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            _logger.LogWarning("Attempted to get localized string with null or empty key");
            return key ?? string.Empty;
        }

        if (_localizedStrings.TryGetValue(key, out var value))
        {
            return value;
        }

        _logger.LogWarning("Localization key not found: {key}", key);
        return $"[{key}]"; // Return key in brackets to indicate missing translation
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);

        if (args == null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to format localized string for key: {key}", key);
            return template;
        }
    }

    public string GetString(string key, Dictionary<string, object> parameters)
    {
        var template = GetString(key);

        if (parameters == null || parameters.Count == 0)
        {
            return template;
        }

        try
        {
            var result = template;

            // Replace named placeholders like {playerName}, {victimName}, etc.
            foreach (var param in parameters)
            {
                var placeholder = $"{{{param.Key}}}";
                
                // Check for format specifiers like {playerName:F2}
                var placeholderWithFormat = $"{{{param.Key}:";
                
                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, param.Value?.ToString() ?? string.Empty);
                }
                else if (result.Contains(placeholderWithFormat))
                {
                    // Find the format specifier
                    var startIndex = result.IndexOf(placeholderWithFormat, StringComparison.Ordinal);
                    if (startIndex >= 0)
                    {
                        var endIndex = result.IndexOf('}', startIndex);
                        if (endIndex > startIndex)
                        {
                            var fullPlaceholder = result.Substring(startIndex, endIndex - startIndex + 1);
                            var formatSpec = result.Substring(startIndex + placeholderWithFormat.Length, 
                                endIndex - startIndex - placeholderWithFormat.Length);
                            
                            // Apply format specifier
                            var formattedValue = param.Value switch
                            {
                                float f => f.ToString(formatSpec),
                                double d => d.ToString(formatSpec),
                                decimal dec => dec.ToString(formatSpec),
                                int i => i.ToString(formatSpec),
                                long l => l.ToString(formatSpec),
                                _ => param.Value?.ToString() ?? string.Empty
                            };
                            
                            result = result.Replace(fullPlaceholder, formattedValue);
                        }
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format localized string with named parameters for key: {key}", key);
            return template;
        }
    }

    public string GetStringOrDefault(string key, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }

        if (_localizedStrings.TryGetValue(key, out var value))
        {
            return value;
        }

        return defaultValue;
    }

    public string GetStringOrDefault(string key, string defaultValue, params object[] args)
    {
        var template = GetStringOrDefault(key, defaultValue);

        if (args == null || args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(template, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Failed to format localized string for key: {key}", key);
            return template;
        }
    }

    public bool HasKey(string key)
    {
        return !string.IsNullOrWhiteSpace(key) && _localizedStrings.ContainsKey(key);
    }

    public void Reload()
    {
        _logger.LogInformation("Reloading locale: {locale}", CurrentLocale);
        LoadLocale(CurrentLocale);
    }

    /// <summary>
    /// Loads a locale file from the locales directory
    /// </summary>
    private void LoadLocale(string locale)
    {
        var localeFile = Path.Combine(_localeDirectory, $"{locale}.json");

        if (!File.Exists(localeFile))
        {
            _logger.LogWarning("Locale file not found: {file}, falling back to EN", localeFile);

            // Try to fall back to English
            if (locale != "EN")
            {
                localeFile = Path.Combine(_localeDirectory, "EN.json");

                if (!File.Exists(localeFile))
                {
                    _logger.LogError("Default locale file (EN.json) not found!");
                    CreateDefaultEnglishLocale();
                    localeFile = Path.Combine(_localeDirectory, "EN.json");
                }
            }
            else
            {
                // Create default English locale if it doesn't exist
                CreateDefaultEnglishLocale();
            }
        }

        try
        {
            var jsonContent = File.ReadAllText(localeFile);
            var localizedData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

            if (localizedData != null)
            {
                _localizedStrings = new Dictionary<string, string>(localizedData, StringComparer.OrdinalIgnoreCase);
                _logger.LogInformation("Loaded {count} localized strings from {file}", _localizedStrings.Count, localeFile);
            }
            else
            {
                _logger.LogWarning("Failed to deserialize locale file: {file}", localeFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load locale file: {file}", localeFile);
        }
    }

    /// <summary>
    /// Creates the default English locale file with all strings
    /// </summary>
    private void CreateDefaultEnglishLocale()
    {
        var defaultStrings = new Dictionary<string, string>
        {
            // Common
            { "common.cooldown", "Please wait {0:F1}s before using this command again." },
            { "common.invalid_steamid", "{red}Error: Invalid SteamID." },
            { "common.error", "{red}An error occurred while spawning poop." },

            // Poop Commands
            { "poop.must_be_alive", "You must be alive to use this command." },
            { "poop.max_per_round", "{red}Max poops per round limit reached!" },
            { "poop.failed_to_spawn", "{red}Failed to spawn poop!" },
            { "poop.no_dead_players", "No dead players nearby. Spawning at your position!" },
            { "poop.spawned_on_player", "{darkred}{0}{default} pooped on {blue}{1}{default} with a {2} poop ({green}{3:F3}{default})!" },
            { "poop.spawned_on_ground", "You pooped on the ground with a {0} poop ({green}{1:F3}{default})." },
            { "poop.spawned_massive", "{gold}WOW! {darkred}{0}{default} just dropped a {1} poop of size {green}{2:F3}{default}!" },
            { "poop.spawned_announcement", "{darkred}{0}{default} spawned a {1} poop ({green}{2:F3}{default})!" },

            // Size Descriptions
            { "size.tiny", "tiny" },
            { "size.small", "small" },
            { "size.medium", "medium" },
            { "size.large", "large" },
            { "size.huge", "huge" },
            { "size.massive", "MASSIVE" },

            // Color Commands
            { "color.disabled", "{red}Color preferences are currently disabled." },
            { "color.menu_error", "{red}Error opening color menu! Please try again." },
            { "color.save_error", "{red}Error saving your color preference. Please try again." },
            { "color.set_rainbow", "{green}Your poop color has been set to {gold}Rainbow Mode 🌈{green}!" },
            { "color.set_rainbow_info", "{lightblue}Your poops will now cycle through all colors!" },
            { "color.set_random", "{green}Your poop color has been set to {gold}Random Mode 🎲{green}!" },
            { "color.set_random_info", "{lightblue}Each poop will be a random color!" },
            { "color.set_normal", "{green}Your poop color has been set to {0}{1}{green}!" },

            // Leaderboard Commands
            { "leaderboard.no_poopers", "{red}No poop records found!" },
            { "leaderboard.error_poopers", "{red}Error getting top poopers!" },
            { "leaderboard.top_poopers_title", "{green}=== Top {0} Poopers ===" },
            { "leaderboard.top_poopers_entry", "{green}#{0}: {darkred}{1} {default}- {green}{2} {default}poops" },
            { "leaderboard.no_victims", "{red}No poop victim records found!" },
            { "leaderboard.error_victims", "{red}Error getting top victims!" },
            { "leaderboard.top_victims_title", "{green}=== Top {0} Poop Victims ===" },
            { "leaderboard.top_victims_entry", "{green}#{0}: {blue}{1} {default}- pooped on {green}{2} {default}times" },

        };

        var localeFile = Path.Combine(_localeDirectory, "EN.json");

        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var jsonContent = JsonSerializer.Serialize(defaultStrings, jsonOptions);
            File.WriteAllText(localeFile, jsonContent);

            _logger.LogInformation("Created default English locale file: {file}", localeFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create default English locale file");
        }
    }
}
