using Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ForgotPasswordSettingsSubMenu : SettingsSubMenu
{
    ImmInputField emailText;

    protected override void Awake()
    {
        base.Awake();

        emailText = transform.Find("Email").GetComponent<ImmInputField>();
    }

    protected override void OnBackPressed()
    {
        SettingsMenu.Instance.Show<LoginSettingsSubMenu>();
    }

    public override void Show()
    {
        base.Show();

        emailText.text = "";
    }

    public void SendLink()
    {
        var email = emailText.text;

        if (string.IsNullOrEmpty(email))
        {
            InformationToast.Instance.Enqueue("باید ایمیلتو وارد کنی.");
            return;
        }

        ConnectionManager.Instance.EndPoint<SystemEndPoint>().SendPasswordRecoveryLink(email);

        SettingsMenu.Instance.HideSubMenus();
        DialogBox.Instance.Show("اگه ایمیلی که وارد کردی درست باشه، برات یه لینک ارسال می‌شه. حواست باشه پوشه اسپم رو هم چک کنی!", "باشه").Ignore();
    }
}
