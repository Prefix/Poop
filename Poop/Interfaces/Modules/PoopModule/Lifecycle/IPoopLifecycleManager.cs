using Prefix.Poop.Interfaces;
using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Interfaces.PoopModule.Lifecycle;

/// <summary>
/// Manages poop entity lifecycle (tracking, lifetime, cleanup)
/// </summary>
internal interface IPoopLifecycleManager : IModule
{
    /// <summary>
    /// Tracks a poop entity and schedules removal if lifetime is configured
    /// </summary>
    /// <param name="entity">The poop entity to track</param>
    void TrackPoop(IBaseEntity entity);

    /// <summary>
    /// Manually removes a tracked poop entity
    /// </summary>
    /// <param name="entity">The poop entity to remove</param>
    void RemovePoop(IBaseEntity entity);

    /// <summary>
    /// Removes all tracked poop entities
    /// </summary>
    void RemoveAllPoops();

    /// <summary>
    /// Gets the current count of tracked poops
    /// </summary>
    int GetTrackedPoopCount();

    /// <summary>
    /// Checks if max poops per round limit has been reached
    /// </summary>
    bool HasReachedMaxPoopsPerRound();
}
