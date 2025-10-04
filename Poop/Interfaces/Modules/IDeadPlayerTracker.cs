using System.Collections.Generic;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Modules.PoopModule;

namespace Prefix.Poop.Interfaces.Modules;

/// <summary>
/// Interface for tracking dead player positions
/// Used for traditional (non-ragdoll) dead player detection
/// </summary>
internal interface IDeadPlayerTracker : IModule
{
    /// <summary>
    /// Gets the dictionary of currently tracked dead players
    /// Key: Player slot, Value: Dead player information
    /// </summary>
    IReadOnlyDictionary<int, DeadPlayerInfo> DeadPlayers { get; }
}
