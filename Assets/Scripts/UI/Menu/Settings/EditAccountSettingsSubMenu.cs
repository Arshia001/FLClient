using Network;
using Network.Types;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EditAccountSettingsSubMenu : SettingsSubMenu
{
    ImmInputField usernameText, emailText, passwordText, repeatPasswordText;

    string originalUsername, originalEmail;

    protected override void Awake()
    {
        base.Awake();

        usernameText = transform.Find("UserName/Input").GetComponent<ImmInputField>();
        emailText = transform.Find("Email/Input").GetComponent<ImmInputField>();
        passwordText = transform.Find("Password/Input").GetComponent<ImmInputField>();
        repeatPasswordText = transform.Find("RepeatPassword/Input").GetComponent<ImmInputField>();
    }

    public override void Show()
    {
        base.Show();

        var td = TransientData.Instance;
        originalUsername = td.UserName;
        originalEmail = td.EmailAddress;

        usernameText.text = originalUsername;
        emailText.text = originalEmail;
        passwordText.text = "";
        repeatPasswordText.text = "";
    }

    public void OK() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var username = usernameText.text;
        var email = emailText.text;
        var password = passwordText.text;
        var password2 = repeatPasswordText.text;

        if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(password2))
        {
            if (password != password2)
            {
                InformationToast.Instance.Enqueue("گذرواژه‌هایی که وارد کردی یکی نیستن.");
                return;
            }
        }

        var ep = ConnectionManager.Instance.EndPoint<SystemEndPoint>();

        if (!string.IsNullOrEmpty(username) && username != originalUsername)
            using (LoadingIndicator.Show(true))
                if (!await ep.SetUsername(username))
                {
                    InformationToast.Instance.Enqueue("این نام کاربری رو یکی قبلا انتخاب کرده.");
                    return;
                }

        originalUsername = username;

        var td = TransientData.Instance;
        td.UserName.Value = username;

        if (!string.IsNullOrEmpty(email) && email != originalEmail)
            using (LoadingIndicator.Show(true))
                switch (await ep.SetEmail(email))
                {
                    case SetEmailResult.EmailAddressInUse:
                        InformationToast.Instance.Enqueue("با این ایمیل قبلا یه حساب کاربری دیگه ثبت شده.");
                        return;

                    case SetEmailResult.InvalidEmailAddress:
                        InformationToast.Instance.Enqueue("ایمیلی که وارد کردی صحیح نیست.");
                        return;
                }

        originalEmail = email;
        td.EmailAddress.Value = email;

        if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(password2))
            using (LoadingIndicator.Show(true))
                switch (await ep.UpdatePassword(password))
                {
                    case SetPasswordResult.PasswordNotComplexEnough:
                        InformationToast.Instance.Enqueue("گذرواژه‌ای که انتخاب کردی به اندازه کافی طولانی نیست.");
                        return;
                }

        SettingsMenu.Instance.HideSubMenus();
    });

    public void Logout() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        if (await DialogBox.Instance.Show("مطمئنی می‌خوای از حسابت خارج شی؟", "آره", "نه") == DialogBox.Result.Yes)
            ProfileHelper.LogOut();
    });
}
