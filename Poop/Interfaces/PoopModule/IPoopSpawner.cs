using System;
using Prefix.Poop.Modules.PoopModule;
using Prefix.Poop.Shared.Models;
using Vector = Sharp.Shared.Types.Vector;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Interfaces.PoopModule;

/// <summary>
/// Event args for when a poop is spawned
/// </summary>
internal sealed class PoopSpawnedInternalEventArgs
{
    public IGameClient? Player { get; set; }
    public Vector Position { get; set; }
    public float Size { get; set; }
    public IGameClient? Victim { get; set; }
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
    /// <param name="pooper"></param>
    /// <returns>Information about the nearest dead player or null</returns>
    DeadPlayerInfo? FindNearestDeadPlayer(IGameClient? pooper);

    /// <summary>
    /// Spawns a poop entity at the specified position (low-level method)
    /// Does not include sounds, messages, or database logging
    /// </summary>
    /// <param name="position">World position to spawn the poop</param>
    /// <param name="size">Size multiplier (-1 for random)</param>
    /// <param name="color">Optional color override</param>
    /// <returns>Spawn result with entity, size, and position</returns>
    SpawnPoopResult SpawnPoop(Vector position, float size = -1.0f, PoopColorPreference? color = null);

    /// <summary>
    /// Spawns a poop with complete logic including sounds, messages, and database logging
    /// Automatically obtains position from player's pawn and finds nearest dead player as victim
    /// Must be called from the main game thread
    /// </summary>
    /// <param name="player">The player spawning the poop</param>
    /// <param name="size">Size (-1 for random)</param>
    /// <param name="colorPreference">Color to use for the poop</param>
    /// <param name="playSounds">Whether to play spawn sounds</param>
    /// <param name="showMessages">Whether to show chat messages</param>
    /// <returns>Spawn result with entity and actual size</returns>
    SpawnPoopResult SpawnPoopWithFullLogic(
        IGameClient? player,
        float size,
        PoopColorPreference colorPreference,
        bool playSounds = true,
        bool showMessages = true);
}