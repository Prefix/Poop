using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Modules.Player;
using Prefix.Poop.Managers.Menu;

namespace Prefix.Poop.Interfaces.Managers;

/// <summary>
/// Manages interactive menus for players
/// </summary>
internal interface IMenuManager : IManager
{
    /// <summary>
    /// Get the currently active menu for a player
    /// </summary>
    IMenuInstance? GetActiveMenu(IGamePlayer player);

    /// <summary>
    /// Close the currently active menu for a player
    /// </summary>
    void CloseActiveMenu(IGamePlayer player);

    /// <summary>
    /// Open a CenterHtmlMenu for a player
    /// </summary>
    void OpenCenterHtmlMenu(IGamePlayer player, CenterHtmlMenu menu);

    /// <summary>
    /// Handle key press for menu navigation
    /// </summary>
    void OnKeyPress(IGamePlayer player, int key);
}
