using Prefix.Poop.Interfaces;
using Prefix.Poop.Managers.Event;
using Microsoft.Extensions.Logging;
using Sharp.Shared.GameEvents;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Modules.EventExample;

internal sealed class ListenEvent : IModule
{
    private readonly ILogger<ListenEvent> _logger;

    public ListenEvent(ILogger<ListenEvent> logger, IEventManager eventManager)
    {
        _logger = logger;

        eventManager.ListenEvent("cs_intermission", OnIntermission);
        eventManager.ListenEvent("player_death",    OnPlayerDeath);
    }

    public bool Init()
        => true;

    private void OnIntermission(IGameEvent e)
    {
        _logger.LogInformation("EventFired: {s}", e.Name);
    }

    private void OnPlayerDeath(IGameEvent ev)
    {
        // [[unlikely]] this should never happen
        if (ev is not IEventPlayerDeath e)
        {
            return;
        }

        _logger.LogInformation("{v} was killed by {k} with {w}",
                               e.VictimController?.PlayerName,
                               e.KillerController?.PlayerName ?? "World",
                               e.Weapon);
    }
}
