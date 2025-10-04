using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Modules.Player;
using Sharp.Shared.Enums;

namespace Prefix.Poop.Managers.Menu;

internal class MenuManager(
    InterfaceBridge bridge,
    ILogger<MenuManager> logger,
    ICommandManager commandManager,
    IEventManager eventManager) : IManager, IMenuManager
{
    private readonly Dictionary<int, IMenuInstance> _activeMenus = new();

    public IMenuInstance? GetActiveMenu(IGamePlayer player)
    {
        return _activeMenus.GetValueOrDefault(player.Slot);
    }

    public void CloseActiveMenu(IGamePlayer player)
    {
        if (_activeMenus.TryGetValue(player.Slot, out var activeMenu))
        {
            activeMenu.Reset();

            // Clear the center display for CenterHtmlMenu
            if (activeMenu is CenterHtmlMenuInstance)
            {
                var controller = bridge.EntityManager.FindPlayerControllerBySlot(player.Client.Slot);
                if (controller is { ConnectedState: PlayerConnectedState.PlayerConnected })
                {
                    eventManager.PrintToCenterHtml(controller, " ", 1);
                }
            }
        }

        _activeMenus.Remove(player.Slot);
    }

    public void OpenCenterHtmlMenu(IGamePlayer player, CenterHtmlMenu menu)
    {
        CloseActiveMenu(player);

        var instance = new CenterHtmlMenuInstance(bridge, eventManager, player, menu);
        _activeMenus[player.Slot] = instance;
        instance.Display();
    }

    public void OnKeyPress(IGamePlayer player, int key)
    {
        GetActiveMenu(player)?.OnKeyPress(player, key);
    }

    public bool Init()
    {
        // Register menu commands for keys 1-9 using chat commands (!1, !2, etc.)
        for (int i = 1; i <= 9; i++)
        {
            int key = i; // Capture variable for closure
            commandManager.AddClientChatCommand($"{key}", (player, _) =>
            {
                OnKeyPress(player, key);
                return ECommandAction.Handled;
            });
        }

        // Register command for key 0
        commandManager.AddClientChatCommand("0", (player, _) =>
        {
            OnKeyPress(player, 0);
            return ECommandAction.Handled;
        });

        logger.LogInformation("MenuManager initialized with menu chat commands (!1-!9, !0)");
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
        _activeMenus.Clear();
    }
}
