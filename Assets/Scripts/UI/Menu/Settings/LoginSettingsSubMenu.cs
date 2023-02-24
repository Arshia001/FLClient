using Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginSettingsSubMenu : SettingsSubMenu
{
    ImmInputField emailText, passwordText;

    protected override void Awake()
    {
        base.Awake();

        emailText = transform.Find("Email/Input").GetComponent<ImmInputField>();
        passwordText = transform.Find("Password/Input").GetComponent<ImmInputField>();
    }

    protected override void OnBackPressed()
    {
        SettingsMenu.Instance.Show<ChooseAccountActionSettingsSubMenu>();
    }

    public override void Show()
    {
        base.Show();

        emailText.text = "";
        passwordText.text = "";
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

        using (LoadingIndicator.Show(true))
        {
            var result = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().Login(email, password);

            if (!result.HasValue)
            {
                InformationToast.Instance.Enqueue("اطلاعاتی که وارد کردی صحیح نیست.");
                return;
            }

            ProfileHelper.ChangeClientID(result.Value);
        }
    });

    public void ForgotPassword()
    {
        SettingsMenu.Instance.Show<ForgotPasswordSettingsSubMenu>();
    }
}
