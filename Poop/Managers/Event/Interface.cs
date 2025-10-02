using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Prefix.Poop.Managers.Event;

internal interface IEventManager
{
    delegate void DelegateOnEventFired(IGameEvent e);

    delegate HookReturnValue<bool> DelegateOnHookEvent(EventHookParams param);

    void HookEvent(string eventName, DelegateOnHookEvent callback);

    void ListenEvent(string eventName, DelegateOnEventFired callback);

    IGameEvent? CreateEvent(string eventName, bool force);

    T? CreateEvent<T>(bool force) where T : class, IGameEvent;
}

internal class EventHookParams
{
    public IGameEvent Event      { get; }
    public bool       ServerOnly { get; private set; }

    public void SetServerOnly()
        => ServerOnly = true;

    public EventHookParams(IGameEvent e, bool serverOnly)
    {
        Event      = e;
        ServerOnly = serverOnly;
    }
}
