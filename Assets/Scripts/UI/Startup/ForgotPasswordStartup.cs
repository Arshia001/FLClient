using LightMessage.Client;
using System;
using UnityEngine;

public class ForgotPasswordStartup : MonoBehaviour
{
    ImmInputField emailText;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        Startup.Instance.ShowLogin();
        return true;
    });

    void Awake()
    {
        emailText = transform.Find("Email").GetComponent<ImmInputField>();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        backStackHandler.MenuShown();
        emailText.text = "";
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        backStackHandler.MenuHidden();
    }

    public void SendLink() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var email = emailText.text;

        if (string.IsNullOrEmpty(email))
        {
            InformationToast.Instance.Enqueue("باید ایمیلتو وارد کنی.");
            return;
        }

        try
        {
            using (LoadingIndicator.Show(true))
                await Startup.Instance.Connect(Network.Types.HandShakeMode.RecoveryEmailRequest, null, email, null, null);
        }
        catch (AuthenticationFailedException)
        {
            await DialogBox.Instance.Show("اگه ایمیلی که وارد کردی درست باشه، برات یه لینک ارسال می‌شه. حواست باشه پوشه اسپم رو هم چک کنی!", "باشه");
            Startup.Instance.ShowLogin();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            InformationToast.Instance.Enqueue("اتصال به سرور با خطا مواجه شد.");
        }
    });
}
