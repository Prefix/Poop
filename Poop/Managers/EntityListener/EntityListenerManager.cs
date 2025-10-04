using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;
using Sharp.Shared.Listeners;
using Sharp.Shared.Types;

namespace Prefix.Poop.Managers.EntityListener;

/// <summary>
/// Manages entity lifecycle events and exposes them as .NET events
/// Implements IEntityListener to hook into ModSharp's entity system
/// </summary>
internal sealed class EntityListenerManager(InterfaceBridge bridge) : IEntityListenerManager, IManager, IEntityListener
{
    public event IEntityListenerManager.EntityDelegate? EntityCreated;
    public event IEntityListenerManager.EntityDelegate? EntityDeleted;
    public event IEntityListenerManager.EntityDelegate? EntitySpawned;
    public event IEntityListenerManager.EntityFollowDelegate? EntityFollowed;

    #region IEntityListener Implementation

    public int ListenerVersion => IEntityListener.ApiVersion;
    public int ListenerPriority => 0;

    public void OnEntityCreated(IBaseEntity entity)
        => EntityCreated?.Invoke(entity);

    public void OnEntityDeleted(IBaseEntity entity)
        => EntityDeleted?.Invoke(entity);

    public void OnEntitySpawned(IBaseEntity entity)
        => EntitySpawned?.Invoke(entity);

    public void OnEntityFollowed(IBaseEntity entity, IBaseEntity? owner)
        => EntityFollowed?.Invoke(entity, owner);

    #endregion

    #region IManager Implementation

    public bool Init()
    {
        bridge.EntityManager.InstallEntityListener(this);
        return true;
    }

    public void Shutdown()
    {
        bridge.EntityManager.RemoveEntityListener(this);
    }

    #endregion
}
