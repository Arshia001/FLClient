using LightMessage.Client;
using System;
using UnityEngine;

public class LoginStartup : MonoBehaviour
{
    ImmInputField emailText, passwordText;

    void Awake()
    {
        emailText = transform.Find("Email/Input").GetComponent<ImmInputField>();
        passwordText = transform.Find("Password/Input").GetComponent<ImmInputField>();
    }

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        Startup.Instance.ShowSelectLoginMethod();
        return true;
    });

    public void Show()
    {
        gameObject.SetActive(true);

        backStackHandler.MenuShown();

        emailText.text = "";
        passwordText.text = "";
    }

    public void Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);
    }

    public void Login() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var email = emailText.text;
        var password = passwordText.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            InformationToast.Instance.Enqueue("باید ایمیل و گذرواژه رو وارد کنی.");
            return;
        }

        try
        {
            using (LoadingIndicator.Show(true))
            {
                var result = await Startup.Instance.Connect(Network.Types.HandShakeMode.EmailAndPassword, null, email, password, null);
                Startup.Instance.SetLoginResult(result);
            }
        }
        catch (AuthenticationFailedException)
        {
            InformationToast.Instance.Enqueue("اطلاعاتی که وارد کردی صحیح نیست.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            InformationToast.Instance.Enqueue("اتصال به سرور با خطا مواجه شد.");
        }
    });

    public void ForgotPassword()
    {
        Startup.Instance.ShowForgotPassword();
    }
}
