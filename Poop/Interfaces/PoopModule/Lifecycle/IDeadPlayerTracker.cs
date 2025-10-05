using System.Collections.Generic;
using Prefix.Poop.Modules.PoopModule;
using Sharp.Shared.Objects;

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
    IReadOnlyDictionary<IGameClient, DeadPlayerInfo> DeadPlayers { get; }
}
