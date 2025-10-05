using System;
using System.Text;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.Managers.Player;
using Sharp.Shared.Enums;
using Sharp.Shared.GameEntities;

namespace Prefix.Poop.Managers.Menu;

internal class CenterHtmlMenu(string title, IMenuManager menuManager) : BaseMenu(title)
{
    public string TitleColor { get; set; } = "yellow";
    public string EnabledColor { get; set; } = "green";
    public string DisabledColor { get; set; } = "grey";
    public string PrevPageColor { get; set; } = "yellow";
    public string NextPageColor { get; set; } = "yellow";
    public string CloseColor { get; set; } = "red";
    public int DisplayDuration { get; set; } = 10; // How long the menu stays visible (in seconds), default 10 seconds
    public double RefreshInterval { get; set; } = 0.05; // How often to refresh the menu (in seconds), default 50ms = 20 times per second
    public int MessageDuration { get; set; } = 1; // How long each individual message stays (in seconds), default 1 second
    public bool ShowKeyNumbers { get; set; } = true; // Whether to show !1, !2, etc. for enabled options

    public override void Open(IGamePlayer player)
    {
        ArgumentNullException.ThrowIfNull(menuManager);
        menuManager.OpenCenterHtmlMenu(player, this);
    }

    public override MenuOption AddMenuOption(string display, Action<IGamePlayer, MenuOption> onSelect, bool disabled = false)
    {
        MenuOption option = new MenuOption(display, disabled, onSelect);
        MenuOptions.Add(option);
        return option;
    }
}

internal class CenterHtmlMenuInstance(InterfaceBridge bridge, IEventManager eventManager, IGamePlayer player, IMenu menu) : BaseMenuInstance(player, menu)
{
    private Guid? _refreshTimerHandle;
    private Guid? _lifetimeTimerHandle;

    public override int NumPerPage => 5; // one less than the actual number of items per page to avoid truncated options
    protected override int MenuItemsPerPage => (Menu.ExitButton ? 0 : 1) + ((HasPrevButton && HasNextButton) ? NumPerPage - 1 : NumPerPage);

    public override void Display()
    {
        IPlayerController? controller = Player.Controller;
        if (controller is not { ConnectedState: PlayerConnectedState.PlayerConnected })
        {
            Reset();
            return;
        }

        if (Menu is not CenterHtmlMenu centerHtmlMenu)
        {
            return;
        }

        // Stop any existing timers
        StopTimers();

        // Start the refresh timer (repeating every RefreshInterval)
        _refreshTimerHandle = bridge.ModSharp.PushTimer(RefreshMenu, centerHtmlMenu.RefreshInterval, GameTimerFlags.Repeatable);

        // Start the lifetime timer (one-shot after DisplayDuration)
        _lifetimeTimerHandle = bridge.ModSharp.PushTimer(() =>
        {
            Close();
            return TimerAction.Stop;
        }, centerHtmlMenu.DisplayDuration);

        // Display immediately
        RefreshMenu();
    }

    private TimerAction RefreshMenu()
    {
        IPlayerController? controller = Player.Controller;
        if (controller is not { ConnectedState: PlayerConnectedState.PlayerConnected })
        {
            StopTimers();
            return TimerAction.Stop;
        }

        if (Menu is not CenterHtmlMenu centerHtmlMenu)
        {
            StopTimers();
            return TimerAction.Stop;
        }

        // Check if player is still valid and connected
        if (!Player.IsValid() || Player.Client.SignOnState < SignOnState.Connected)
        {
            StopTimers();
            return TimerAction.Stop;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"<b><font color='{centerHtmlMenu.TitleColor}'>{centerHtmlMenu.Title}</font></b>");
        builder.AppendLine("<br>");

        int keyOffset = 1;

        for (int i = CurrentOffset; i < Math.Min(CurrentOffset + MenuItemsPerPage, centerHtmlMenu.MenuOptions.Count); i++)
        {
            MenuOption option = centerHtmlMenu.MenuOptions[i];

            if (option.Disabled)
            {
                // Disabled option - no number prefix, entire text in gray
                builder.Append($"<font color='{centerHtmlMenu.DisabledColor}'>{option.Text}</font>");
            }
            else
            {
                // Enabled option - conditionally show number prefix based on ShowKeyNumbers
                if (centerHtmlMenu.ShowKeyNumbers)
                {
                    builder.Append($"<font color='{centerHtmlMenu.EnabledColor}'>!{keyOffset}</font> {option.Text}");
                }
                else
                {
                    builder.Append($"<font color='{centerHtmlMenu.EnabledColor}'>{option.Text}</font>");
                }
                keyOffset++;
            }

            builder.AppendLine("<br>");
        }

        if (HasPrevButton)
        {
            builder.AppendFormat($"<font color='{centerHtmlMenu.PrevPageColor}'>!7</font> &#60;- Prev");
            builder.AppendLine("<br>");
        }

        if (HasNextButton)
        {
            builder.AppendFormat($"<font color='{centerHtmlMenu.NextPageColor}'>!8</font> -> Next");
            builder.AppendLine("<br>");
        }

        if (centerHtmlMenu.ExitButton)
        {
            builder.AppendFormat($"<font color='{centerHtmlMenu.CloseColor}'>!9</font> -> Close");
            builder.AppendLine("<br>");
        }

        string currentPageText = builder.ToString();

        // Send the menu - use MessageDuration for each individual message
        // This should be longer than RefreshInterval to avoid flashing
        eventManager.PrintToCenterHtml(controller, currentPageText, centerHtmlMenu.MessageDuration);

        return TimerAction.Continue;
    }

    private void StopTimers()
    {
        if (_refreshTimerHandle.HasValue && bridge.ModSharp.IsValidTimer(_refreshTimerHandle.Value))
        {
            bridge.ModSharp.StopTimer(_refreshTimerHandle.Value);
            _refreshTimerHandle = null;
        }

        if (_lifetimeTimerHandle.HasValue && bridge.ModSharp.IsValidTimer(_lifetimeTimerHandle.Value))
        {
            bridge.ModSharp.StopTimer(_lifetimeTimerHandle.Value);
            _lifetimeTimerHandle = null;
        }
    }

    public override void Close()
    {
        // Stop all timers
        StopTimers();

        // Send a blank message to clear the menu
        IPlayerController? controller = Player.Controller;
        if (controller is not { ConnectedState: PlayerConnectedState.PlayerConnected })
        {
            return;
        }

        eventManager.PrintToCenterHtml(controller, " ", 1);
    }

    public override void Reset()
    {
        StopTimers();
        base.Reset();
    }
}
