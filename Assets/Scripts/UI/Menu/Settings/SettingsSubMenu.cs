using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class SettingsSubMenu : MonoBehaviour, IMenu
{
    MenuBackStackHandler backStackHandler;

    protected virtual void Awake()
    {
        backStackHandler = new MenuBackStackHandler(() =>
        {
            OnBackPressed();
            return true;
        });
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
        backStackHandler.MenuShown();
    }

    public virtual Task Hide()
    {
        gameObject.SetActive(false);
        backStackHandler.MenuHidden();

        return Task.CompletedTask;
    }

    protected virtual void OnBackPressed() => SettingsMenu.Instance.HideSubMenus();
}
