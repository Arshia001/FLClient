using Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : SingletonBehaviour<MenuManager>
{
    MenuGroup<INonGameMenu> menus;

    void Start()
    {
#if UNITY_EDITOR
        if (ConnectionManager.Instance == null)
        {
            SceneManager.LoadScene(0);
            return;
        }
#endif

        menus = new MenuGroup<INonGameMenu>(gameObject);
        TaskExtensions.RunIgnoreAsync(menus.Show<MainMenu>);
        Footer.Instance.SetActivePageWithoutAnimation(Footer.FooterIconType.Home);

        NavigateBackStack.Instance.Push(() =>
        {
            TaskExtensions.RunIgnoreAsync(async () => 
            {
                if (await DialogBox.Instance.Show("مطمئنی می‌خوای از بازی خارج بشی؟", "آره", "نه", AdRepository.AdZone.ExitConfirmation) == DialogBox.Result.Yes)
                    Application.Quit();
            });

            return false;
        });
    }

    public Task Show<T>() where T : MonoBehaviour, INonGameMenu => menus.Show<T>();

    public T Menu<T>() where T : MonoBehaviour, INonGameMenu => menus.Menu<T>();

    public void HideAll() => TaskExtensions.RunIgnoreAsync(menus.Hide);

    public void ShowHome() => TaskExtensions.RunIgnoreAsync(Show<MainMenu>);

    public void ShowSettings() => TaskExtensions.RunIgnoreAsync(Show<SettingsMenu>);

    public void ShowLeaderBoard() => TaskExtensions.RunIgnoreAsync(Show<LeaderBoardMenu>);

    public void ShowProfile() => TaskExtensions.RunIgnoreAsync(Show<ProfileMenu>);

    public void ShowShop() => TaskExtensions.RunIgnoreAsync(Show<ShopMenu>);

    public void ShowAvatarShop()
    {
        ShowShop();
        menus.Menu<ShopMenu>().ShowAvatarShop();
    }
}
