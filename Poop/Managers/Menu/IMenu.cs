using System;
using System.Collections.Generic;
using Prefix.Poop.Interfaces.Modules.Player;

namespace Prefix.Poop.Managers.Menu;

public enum PostSelectAction
{
    Close,
    Reset,
    Nothing
}

public interface IMenu
{
    string Title { get; set; }
    List<MenuOption> MenuOptions { get; }
    PostSelectAction PostSelectAction { get; set; }
    bool ExitButton { get; set; }

    MenuOption AddMenuOption(string display, Action<IGamePlayer, MenuOption> onSelect, bool disabled = false);
    void Open(IGamePlayer player);
}

public interface IMenuInstance
{
    IMenu Menu { get; }
    IGamePlayer Player { get; }
    bool CloseOnSelect { get; set; }
    int Page { get; set; }
    int CurrentOffset { get; set; }
    int NumPerPage { get; }

    void NextPage();
    void PrevPage();
    void Reset();
    void Close();
    void Display();
    void OnKeyPress(IGamePlayer player, int key);
}
