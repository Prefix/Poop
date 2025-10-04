using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules.Player;
using Sharp.Shared.Enums;
using Sharp.Shared.Listeners;
using Sharp.Shared.Managers;
using Sharp.Shared.Objects;
using Sharp.Shared.Types;

namespace Prefix.Poop.Managers.Commands;

internal class CommandManager : ICommandManager, IClientListener
{
    private readonly Dictionary<string, ICommandManager.ClientCommandDelegate> _adminChatCommands;
    private readonly InterfaceBridge _bridge;

    private readonly Dictionary<string, Func<StringCommand, ECommandAction>> _serverCommands;

    private readonly Dictionary<string, ICommandManager.ClientCommandDelegate> _clientChatCommands;
    private readonly Dictionary<string, IClientManager.DelegateClientCommand> _clientCommandListeners;
    private readonly FrozenSet<char> _commandTriggers;

    private readonly ILogger<CommandManager> _logger;

    private readonly IPlayerManager _playerManager;

    public CommandManager(InterfaceBridge bridge, ILogger<CommandManager> logger, IPlayerManager playerManager)
    {
        _bridge = bridge;
        _logger = logger;
        _playerManager = playerManager;

        _clientChatCommands = [];
        _clientCommandListeners = [];
        _adminChatCommands = [];
        _serverCommands = [];

        HashSet<char> set = ['!', '/', '.', '！', '．', '／', '。'];
        _commandTriggers = set.ToFrozenSet();
    }

    public int ListenerVersion => IGameListener.ApiVersion;
    public int ListenerPriority => 10;

    public ECommandAction OnClientSayCommand(IGameClient client, bool teamOnly, bool isCommand, string commandName,
                                             string message)
    {
        if (message.Distinct().Count() == 1 || !_commandTriggers.Contains(message[0]))
        {
            return ECommandAction.Skipped;
        }

        var player = _playerManager.GetPlayer(client);

        // Player not found or not valid yet
        if (player is null)
        {
            return ECommandAction.Skipped;
        }

        var split = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var rawCommand = split[0][1..].ToLowerInvariant();

        var startIndex = rawCommand.Length + 1;
        var arguments = message.Length > startIndex ? message[startIndex..] : null;

        if (_clientChatCommands.TryGetValue(rawCommand, out var callback))
        {
            return callback(player, new StringCommand(rawCommand, true, arguments));
        }

        if (_adminChatCommands.TryGetValue(rawCommand, out callback))
        {
            if (!player.IsAdmin)
            {
                return ECommandAction.Handled;
            }

            return callback(player, new StringCommand(rawCommand, true, arguments));
        }

        return ECommandAction.Skipped;
    }

    public void AddClientChatCommand(string command, ICommandManager.ClientCommandDelegate handler)
    {
        if (_clientChatCommands.TryAdd(command, handler))
        {
            return;
        }

        _logger.LogWarning("{cmd} is already added in _clientChatCommands.", command);
    }

    public void AddAdminChatCommand(string command, ICommandManager.ClientCommandDelegate handler)
    {
        if (_adminChatCommands.TryAdd(command, handler))
        {
            return;
        }

        _logger.LogWarning("{cmd} is already added in _adminChatCommands.", command);
    }

    public void AddClientCommandListener(string command, IClientManager.DelegateClientCommand handler)
    {
        if (_clientCommandListeners.TryAdd(command, handler))
        {
            _bridge.ClientManager.InstallCommandListener(command, handler);

            return;
        }

        _logger.LogWarning("{cmd} is already added in _clientCommandListeners.", command);
    }

    public void AddServerCommand(string command, Func<StringCommand, ECommandAction> handler)
    {
        if (_serverCommands.TryAdd(command, handler))
        {
            _bridge.ConVarManager.CreateServerCommand(command, handler);
        }
    }

    public bool Init()
    {
        _bridge.ClientManager.InstallClientListener(this);

        return true;
    }

    public void OnPostInit()
    {
    }

    public void OnAllSharpModulesLoaded()
    {
    }

    public void Shutdown()
    {
        foreach (var (command, handler) in _clientCommandListeners)
        {
            _bridge.ClientManager.RemoveCommandListener(command, handler);
        }

        foreach (var (command, _) in _serverCommands)
        {
            _bridge.ConVarManager.ReleaseCommand(command);
        }

        _bridge.ClientManager.RemoveClientListener(this);
    }
}
