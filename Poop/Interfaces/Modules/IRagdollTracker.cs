using System.Collections.Generic;
using Prefix.Poop.Interfaces;
using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Interfaces.Modules;

/// <summary>
/// Interface for tracking ragdoll entities
/// </summary>
internal interface IRagdollTracker : IModule
{
    /// <summary>
    /// Gets the read-only dictionary of tracked ragdolls by player slot
    /// Key: Player slot
    /// Value: Ragdoll entity
    /// </summary>
    IReadOnlyDictionary<int, IBaseEntity> Ragdolls { get; }
}
