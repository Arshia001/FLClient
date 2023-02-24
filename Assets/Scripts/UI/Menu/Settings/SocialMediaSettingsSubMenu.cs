using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SocialMediaSettingsSubMenu : SettingsSubMenu
{
    [SerializeField] string instagramUrl = null;
    [SerializeField] string telegramUrl = null;
    [SerializeField] string supportEmail = null;

    public void Instagram() => Application.OpenURL(instagramUrl);
    
    public void Telegram() => Application.OpenURL(telegramUrl);
    
    public void SupportEmail() => Application.OpenURL(supportEmail);
}
