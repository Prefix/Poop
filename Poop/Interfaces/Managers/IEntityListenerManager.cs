using Prefix.Poop.Interfaces;
using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Interfaces.Managers;

/// <summary>
/// Interface for entity lifecycle event listeners
/// </summary>
internal interface IEntityListenerManager : IManager
{
    delegate void EntityDelegate(IBaseEntity entity);
    delegate void EntityFollowDelegate(IBaseEntity entity, IBaseEntity? owner);

    event EntityDelegate? EntityCreated;
    event EntityDelegate? EntityDeleted;
    event EntityDelegate? EntitySpawned;
    event EntityFollowDelegate? EntityFollowed;
}
