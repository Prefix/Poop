using System.Collections.Generic;
using Prefix.Poop.Modules.PoopModule;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Interfaces.PoopModule.Lifecycle;

/// <summary>
/// Interface for tracking ragdoll entities
/// </summary>
internal interface IRagdollTracker : IModule
{
    /// <summary>
    /// Gets the read-only dictionary of tracked ragdolls by game client
    /// Key: Game client
    /// Value: Ragdoll information
    /// </summary>
    IReadOnlyDictionary<IGameClient, RagdollInfo> Ragdolls { get; }
}
