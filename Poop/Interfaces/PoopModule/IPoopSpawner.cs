using System;
using Prefix.Poop.Modules.PoopModule;
using Prefix.Poop.Shared.Models;
using Vector = Sharp.Shared.Types.Vector;

namespace Prefix.Poop.Interfaces.PoopModule;

/// <summary>
/// Event args for when a poop is spawned
/// </summary>
internal sealed class PoopSpawnedInternalEventArgs
{
    public string PlayerSteamId { get; set; } = string.Empty;
    public Vector Position { get; set; }
    public float Size { get; set; }
    public string? VictimName { get; set; }
    public string? VictimSteamId { get; set; }
    public bool Success { get; set; }
}

/// <summary>
/// Interface for spawning poop entities with physics, colors, and size variations
/// </summary>
internal interface IPoopSpawner : IModule
{
    /// <summary>
    /// Event fired after a poop spawn attempt (success or failure)
    /// Used internally by SharedInterface to fire public API events
    /// </summary>
    event Action<PoopSpawnedInternalEventArgs>? PoopSpawnedInternal;

    /// <summary>
    /// Finds the nearest dead player to a position
    /// </summary>
    /// <param name="position">Position to search from</param>
    /// <param name="pooperSlot">Slot of the player doing the pooping (to exclude)</param>
    /// <returns>Information about the nearest dead player or null</returns>
    DeadPlayerInfo? FindNearestDeadPlayer(Vector position, int pooperSlot);

    /// <summary>
    /// Generates a random poop size based on the configured rarity system
    /// </summary>
    /// <returns>Random poop size between MinPoopSize and MaxPoopSize</returns>
    float GetRandomPoopSize();

    /// <summary>
    /// Spawns a poop with complete logic including sounds, messages, and database logging
    /// Must be called from the main game thread
    /// </summary>
    /// <param name="playerSteamId">Steam ID of the player spawning the poop</param>
    /// <param name="position">World position to spawn poop</param>
    /// <param name="size">Size (-1 for random)</param>
    /// <param name="colorPreference">Color to use for the poop</param>
    /// <param name="victimName">Optional victim name</param>
    /// <param name="victimSteamId">Optional victim Steam ID</param>
    /// <param name="playSounds">Whether to play spawn sounds</param>
    /// <param name="showMessages">Whether to show chat messages</param>
    /// <returns>Spawn result with entity and actual size</returns>
    SpawnPoopResult SpawnPoopWithFullLogic(
        string playerSteamId,
        Vector position,
        float size,
        PoopColorPreference colorPreference,
        string? victimName = null,
        string? victimSteamId = null,
        bool playSounds = true,
        bool showMessages = true);
}