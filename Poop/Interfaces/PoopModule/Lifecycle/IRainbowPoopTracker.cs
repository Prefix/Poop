using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Interfaces.PoopModule.Lifecycle;

/// <summary>
/// Interface for tracking and managing rainbow-colored poop entities
/// </summary>
internal interface IRainbowPoopTracker : IModule
{
    /// <summary>
    /// Tracks a rainbow poop entity for color cycling
    /// </summary>
    void TrackRainbowPoop(IBaseEntity entity);

    /// <summary>
    /// Stops tracking a specific rainbow poop
    /// </summary>
    void StopTracking(IBaseEntity entity);

    /// <summary>
    /// Clears all tracked rainbow poops (e.g., on round end)
    /// </summary>
    void ClearAll();

    /// <summary>
    /// Gets the number of currently tracked rainbow poops
    /// </summary>
    int GetTrackedCount();
}
