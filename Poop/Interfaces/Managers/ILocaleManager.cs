using System;
using System.Collections.Generic;
using Prefix.Poop.Interfaces;

namespace Prefix.Poop.Interfaces.Managers;

/// <summary>
/// Locale manager interface for managing localized strings and translations
/// </summary>
internal interface ILocaleManager : IManager
{
    /// <summary>
    /// Gets the current locale/language code (e.g., "en-US", "es-ES", "de-DE")
    /// </summary>
    string CurrentLocale { get; }

    /// <summary>
    /// Gets a localized string by key
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <returns>The localized string</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string by key with format arguments (positional placeholders like {0}, {1})
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <param name="args">Format arguments to replace placeholders</param>
    /// <returns>The formatted localized string</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Gets a localized string by key with named parameters (e.g., {playerName}, {victimName})
    /// This allows translators to reorder parameters in different languages
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <param name="parameters">Dictionary of named parameters</param>
    /// <returns>The formatted localized string</returns>
    string GetString(string key, Dictionary<string, object> parameters);

    /// <summary>
    /// Gets a localized string by key, or returns the default value if key not found
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <param name="defaultValue">The default value if key not found</param>
    /// <returns>The localized string or default value</returns>
    string GetStringOrDefault(string key, string defaultValue);

    /// <summary>
    /// Gets a localized string by key with format arguments, or returns the default value if key not found
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <param name="defaultValue">The default value if key not found</param>
    /// <param name="args">Format arguments to replace placeholders</param>
    /// <returns>The formatted localized string or default value</returns>
    string GetStringOrDefault(string key, string defaultValue, params object[] args);

    /// <summary>
    /// Checks if a localization key exists
    /// </summary>
    /// <param name="key">The localization key</param>
    /// <returns>True if the key exists, false otherwise</returns>
    bool HasKey(string key);

    /// <summary>
    /// Reloads the localization resources from disk
    /// </summary>
    void Reload();
}
