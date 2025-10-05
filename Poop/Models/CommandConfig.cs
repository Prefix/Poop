namespace Prefix.Poop.Models;

/// <summary>
/// Configuration for a command or group of command aliases
/// </summary>
internal sealed class CommandConfig
{
    /// <summary>
    /// Whether this command is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Command aliases (e.g., "poop", "shit")
    /// </summary>
    public string[] Aliases { get; set; } = [];

    /// <summary>
    /// Cooldown in seconds for this specific command
    /// </summary>
    public int CooldownSeconds { get; set; } = 3;
}
