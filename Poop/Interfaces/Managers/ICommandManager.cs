using System;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers.Player;
using Sharp.Shared.Enums;
using Sharp.Shared.Types;

namespace Prefix.Poop.Interfaces.Managers;

internal interface ICommandManager : IManager
{
    delegate ECommandAction ClientCommandDelegate(IGamePlayer player, StringCommand command);

    void AddClientChatCommand(string command, ClientCommandDelegate handler);

    void AddAdminChatCommand(string command, ClientCommandDelegate handler);

    void AddServerCommand(string command, Func<StringCommand, ECommandAction> handler);
}
