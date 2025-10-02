using Sharp.Shared.GameEntities;
using Sharp.Shared.Objects;

namespace Prefix.Poop.PoopAPI;

public interface IPoopAPI
{
    static string Identity => typeof(IPoopAPI).FullName ?? nameof(IPoopAPI);

    void Hello(IGameClient client);

    void Idle(IPlayerController controller);

    void Kick(IPlayerPawn pawn);
}
