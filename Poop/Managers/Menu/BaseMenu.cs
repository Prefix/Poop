using System;
using System.Collections.Generic;
using Prefix.Poop.Interfaces.Managers.Player;

namespace Prefix.Poop.Managers.Menu;

public class MenuOption
{
    public string Text { get; set; }
    public bool Disabled { get; set; }
    public Action<IGamePlayer, MenuOption> OnSelect { get; set; }

    public MenuOption(string display, bool disabled, Action<IGamePlayer, MenuOption> onSelect)
    {
        Text = display;
        Disabled = disabled;
        OnSelect = onSelect;
    }
}

public abstract class BaseMenu : IMenu
{
    public string Title { get; set; }
    public List<MenuOption> MenuOptions { get; } = new();
    public PostSelectAction PostSelectAction { get; set; } = PostSelectAction.Reset;
    public bool ExitButton { get; set; } = true;

    protected BaseMenu(string title)
    {
        Title = title;
    }

    public virtual MenuOption AddMenuOption(string display, Action<IGamePlayer, MenuOption> onSelect, bool disabled = false)
    {
        var option = new MenuOption(display, disabled, onSelect);
        MenuOptions.Add(option);
        return option;
    }

    public abstract void Open(IGamePlayer player);
}

public abstract class BaseMenuInstance : IMenuInstance
{
    public virtual int NumPerPage => 6;
    public bool CloseOnSelect { get; set; } = true;
    public Stack<int> PrevPageOffsets { get; } = new();
    public IMenu Menu { get; }
    public IGamePlayer Player { get; }
    public int Page { get; set; }
    public int CurrentOffset { get; set; }

    protected BaseMenuInstance(IGamePlayer player, IMenu menu)
    {
        Menu = menu;
        Player = player;
    }

    protected bool HasPrevButton => Page > 0;
    protected bool HasNextButton => Menu.MenuOptions.Count > NumPerPage && CurrentOffset + NumPerPage < Menu.MenuOptions.Count;
    protected bool HasExitButton => Menu.ExitButton;
    protected virtual int MenuItemsPerPage => NumPerPage;

    public virtual void Display()
    {
        throw new NotImplementedException();
    }

    public void OnKeyPress(IGamePlayer player, int key)
    {
        if (player.Slot != Player.Slot) return;

        // Key 8 = Next Page
        if (key == 8 && HasNextButton)
        {
            NextPage();
            return;
        }

        // Key 7 = Previous Page
        if (key == 7 && HasPrevButton)
        {
            PrevPage();
            return;
        }

        // Key 9 = Exit/Close
        if (key == 9 && HasExitButton)
        {
            Close();
            return;
        }

        // Handle menu item selection (keys 1-6 typically)
        var desiredValue = key;
        var menuItemIndex = CurrentOffset + desiredValue - 1;

        if (Menu?.MenuOptions == null)
            return;

        if (menuItemIndex >= 0 && menuItemIndex < Menu.MenuOptions.Count)
        {
            var menuOption = Menu.MenuOptions[menuItemIndex];

            if (!menuOption.Disabled)
            {
                // Redisplay the menu so player sees what they selected
                Display();

                // Execute the selection action
                menuOption.OnSelect(Player, menuOption);

                // Handle post-select action
                switch (Menu.PostSelectAction)
                {
                    case PostSelectAction.Close:
                        Close();
                        break;
                    case PostSelectAction.Reset:
                        Reset();
                        break;
                    case PostSelectAction.Nothing:
                        // Do nothing
                        break;
                    default:
                        throw new NotImplementedException("The specified Select Action is not supported!");
                }
            }
        }
    }

    public virtual void Reset()
    {
        CurrentOffset = 0;
        Page = 0;
        PrevPageOffsets.Clear();
    }

    public abstract void Close();

    public void NextPage()
    {
        PrevPageOffsets.Push(CurrentOffset);
        CurrentOffset += MenuItemsPerPage;
        Page++;
        Display();
    }

    public void PrevPage()
    {
        Page--;
        CurrentOffset = PrevPageOffsets.Pop();
        Display();
    }
}
