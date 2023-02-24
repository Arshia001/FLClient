using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : MonoBehaviour, INonGameMenu
{
    [SerializeField] Sprite activeTab = default, inactiveTab = default;
    
    Image coinTab, avatarTab;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        TaskExtensions.RunIgnoreAsync(MenuManager.Instance.Show<MainMenu>);
        return true;
    });

    public CoinShopMenu CoinShop { get; private set; }
    public AvatarShopMenu AvatarShop { get; private set; }

    public RectTransform AvatarShopButtonTransform => avatarTab.rectTransform;

    void Awake()
    {
        CoinShop = transform.Find("CoinShop").GetComponent<CoinShopMenu>();
        AvatarShop = transform.Find("AvatarShop").GetComponent<AvatarShopMenu>();

        coinTab = transform.Find("Tabs/Coin").GetComponent<Image>();
        avatarTab = transform.Find("Tabs/Avatar").GetComponent<Image>();
    }

    public async Task Hide()
    {
        await AvatarShop.ShowExitConfirmationIfNeeded();

        backStackHandler.MenuHidden();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Footer.Instance.SetActivePage(Footer.FooterIconType.Shop);

        backStackHandler.MenuShown();

        AvatarShop.ResetUI();

        ShowCoinShop();
    }

    public void ShowCoinShop()
    {
        CoinShop.SetVisible(true);
        AvatarShop.SetVisible(false);

        coinTab.sprite = activeTab;
        avatarTab.sprite = inactiveTab;
    }

    public void ShowAvatarShop()
    {
        CoinShop.SetVisible(false);
        AvatarShop.SetVisible(true);

        coinTab.sprite = inactiveTab;
        avatarTab.sprite = activeTab;
    }
}
