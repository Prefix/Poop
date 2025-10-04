// ReSharper disable UnusedMember.Global
using Sharp.Shared.Types;

namespace Prefix.Poop.Shared.Events;

/// <summary>
/// Event arguments for when a player executes a poop command
/// Can be cancelled to prevent the command from executing
/// Fires BEFORE any validation (cooldown, alive check, etc.)
/// </summary>
public sealed class PoopCommandEventArgs
{
    /// <summary>
    /// The player who executed the command (SteamID64)
    /// </summary>
    public string PlayerSteamId { get; set; } = string.Empty;

    /// <summary>
    /// The player's name
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// The command that was executed (e.g., "poop", "shit", "poop_size", "poop_rnd")
    /// </summary>
    public string CommandName { get; set; } = string.Empty;

    /// <summary>
    /// Set to true to block the command from executing
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
/// Event arguments for when a poop is about to be spawned
/// Can be cancelled to prevent spawn
/// </summary>
public sealed class PoopSpawnEventArgs
{
    /// <summary>
    /// The player who is spawning the poop (SteamID64)
    /// </summary>
    public string PlayerSteamId { get; set; } = string.Empty;

    /// <summary>
    /// The player's name
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Position where the poop will be spawned
    /// </summary>
    public Vector Position { get; set; }

    /// <summary>
    /// Size of the poop that will be spawned
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// Name of the victim (dead player) if pooping on someone
    /// </summary>
    public string? VictimName { get; set; }

    /// <summary>
    /// SteamID of the victim if pooping on someone
    /// </summary>
    public string? VictimSteamId { get; set; }

    /// <summary>
    /// Whether this spawn was triggered by a command (true) or programmatically via API (false)
    /// </summary>
    public bool IsCommandTriggered { get; set; }

    /// <summary>
    /// Set to true to cancel the poop spawn
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// Optional reason for cancellation (shown to player if Cancel = true)
    /// </summary>
    public string? CancelReason { get; set; }
}

/// <summary>
/// Event arguments for after a poop has been spawned
/// </summary>
public sealed class PoopSpawnedEventArgs
{
    /// <summary>
    /// The player who spawned the poop (SteamID64)
    /// </summary>
    public string PlayerSteamId { get; set; } = string.Empty;

    /// <summary>
    /// The player's name
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    /// Position where the poop was spawned
    /// </summary>
    public Vector Position { get; set; }

    /// <summary>
    /// Size of the spawned poop
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// Name of the victim (dead player) if pooped on someone
    /// </summary>
    public string? VictimName { get; set; }

    /// <summary>
    /// SteamID of the victim if pooped on someone
    /// </summary>
    public string? VictimSteamId { get; set; }

    /// <summary>
    /// Whether this spawn was triggered by a command (true) or programmatically via API (false)
    /// </summary>
    public bool IsCommandTriggered { get; set; }

    /// <summary>
    /// Whether the spawn was successful
    /// </summary>
    public bool Success { get; set; }
}
