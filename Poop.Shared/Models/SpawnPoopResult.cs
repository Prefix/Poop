using Sharp.Shared.GameEntities;
using Sharp.Shared.Types;

namespace Prefix.Poop.Shared.Models;

/// <summary>
/// Result of spawning a poop entity
/// </summary>
public sealed class SpawnPoopResult
{
    /// <summary>
    /// The spawned poop entity (null if spawn failed)
    /// </summary>
    public IBaseModelEntity? Entity { get; set; }

    /// <summary>
    /// The actual size of the spawned poop
    /// </summary>
    public float Size { get; set; }

    /// <summary>
    /// The position where the poop was spawned
    /// </summary>
    public Vector Position { get; set; }

    /// <summary>
    /// Name of the victim (dead player) if poop was spawned on them
    /// </summary>
    public string? VictimName { get; set; }

    /// <summary>
    /// Whether the spawn was successful
    /// </summary>
    public bool Success => Entity != null;
}
