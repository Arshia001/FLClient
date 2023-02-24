using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Network.Types;
using UnityEngine;

public class SettingsMenu : SingletonBehaviour<SettingsMenu>, INonGameMenu
{
    SubMenuGroupWithContainer<SettingsSubMenu> subMenus;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        TaskExtensions.RunIgnoreAsync(MenuManager.Instance.Show<MainMenu>);
        return true;
    });

    public RectTransform AccountSettingsButtonTransform => transform.Find("Buttons/AccountSettings") as RectTransform;
    public RectTransform CreateSubjectButtonTransform => transform.Find("Buttons/CreateSubject") as RectTransform;

    protected override void Awake()
    {
        base.Awake();

        subMenus = new SubMenuGroupWithContainer<SettingsSubMenu>(transform.Find("SubMenus").gameObject);
    }

    public Task Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);

        return Task.CompletedTask;
    }

    public void Show()
    {
        backStackHandler.MenuShown();
        gameObject.SetActive(true);
        TaskExtensions.RunIgnoreAsync(subMenus.HideAll);
        Footer.Instance.SetActivePage(Footer.FooterIconType.Settings);
    }

    public void Show<T>() where T : SettingsSubMenu => TaskExtensions.RunIgnoreAsync(subMenus.Show<T>);

    public void CloseSubMenu() => TaskExtensions.RunIgnoreAsync(subMenus.HideAll);

    public void ShowSocialMedia() => Show<SocialMediaSettingsSubMenu>();

    public void ShowAccount()
    {
        switch (TransientData.Instance.RegistrationStatus.Value)
        {
            case RegistrationStatus.EmailAndPassword:
                Show<EditAccountSettingsSubMenu>();
                break;
            case RegistrationStatus.BazaarToken:
                Show<EditBazaarAccountSettingsSubMenu>();
                break;
            case RegistrationStatus.Unregistered:
                Show<ChooseAccountActionSettingsSubMenu>();
                break;
            default:
                Debug.LogError("Unknown registration status " + TransientData.Instance.RegistrationStatus.Value.ToString());
                break;
        }
    }

    public void ShowConfiguration() => Show<ConfigurationSettingsSubMenu>();

    public void ShowCreateSubject() => Show<CreateSubjectSettingsSubMenu>();

    public void HideSubMenus() => TaskExtensions.RunIgnoreAsync(subMenus.HideAll);
}
