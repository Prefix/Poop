using Sharp.Shared.Enums;
using Sharp.Shared.Objects;

namespace Prefix.Poop.Interfaces.Managers;

/// <summary>
/// Interface for client lifecycle event listeners
/// </summary>
internal interface IClientListenerManager : IManager
{
    delegate void ClientDelegate(IGameClient client);
    delegate void ClientDisconnectDelegate(IGameClient client, NetworkDisconnectionReason reason);
    delegate ECommandAction ClientSayDelegate(IGameClient client, bool teamOnly, bool isCommand, string commandName, string message);
    delegate bool ClientPreAdminCheckDelegate(IGameClient client);

    event ClientPreAdminCheckDelegate? ClientPreAdminCheck;
    event ClientDelegate? ClientConnected;
    event ClientDelegate? ClientPutInServer;
    event ClientDelegate? ClientPostAdminCheck;
    event ClientDisconnectDelegate? ClientDisconnecting;
    event ClientDisconnectDelegate? ClientDisconnected;
    event ClientDelegate? ClientSettingChanged;
    event ClientSayDelegate? ClientSayCommand;
}
