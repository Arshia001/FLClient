using GameAnalyticsSDK;
using Network;
using Network.Types;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreateAccountSettingsSubMenu : SettingsSubMenu
{
    ImmInputField usernameText, emailText, passwordText, repeatPasswordText, inviteCodeText;

    protected override void Awake()
    {
        base.Awake();

        usernameText = transform.Find("UserName/Input").GetComponent<ImmInputField>();
        emailText = transform.Find("Email/Input").GetComponent<ImmInputField>();
        passwordText = transform.Find("Password/Input").GetComponent<ImmInputField>();
        repeatPasswordText = transform.Find("RepeatPassword/Input").GetComponent<ImmInputField>();
        inviteCodeText = transform.Find("InviteCode/Input").GetComponent<ImmInputField>();
    }

    protected override void OnBackPressed()
    {
        SettingsMenu.Instance.Show<ChooseAccountActionSettingsSubMenu>();
    }

    public override void Show()
    {
        base.Show();

        usernameText.text = "";
        emailText.text = "";
        passwordText.text = "";
        repeatPasswordText.text = "";
        inviteCodeText.text = "";
    }

    public void OK() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var username = usernameText.text;
        var email = emailText.text;
        var password = passwordText.text;
        var password2 = repeatPasswordText.text;
        var inviteCode = inviteCodeText.text.Trim().ToUpperInvariant();
        var haveInviteCode = !string.IsNullOrWhiteSpace(inviteCode);

        if (string.IsNullOrEmpty(username))
        {
            InformationToast.Instance.Enqueue("باید نام کاربریت رو وارد کنی.");
            return;
        }

        if (string.IsNullOrEmpty(email))
        {
            InformationToast.Instance.Enqueue("باید ایمیلت رو وارد کنی.");
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            InformationToast.Instance.Enqueue("باید گذرواژه رو وارد کنی.");
            return;
        }

        if (password != password2)
        {
            InformationToast.Instance.Enqueue("گذرواژه‌هایی که وارد کردی یکی نیستن.");
            return;
        }

        using (LoadingIndicator.Show(true))
        {
            var (result, totalGold) = await ConnectionManager.Instance.EndPoint<SystemEndPoint>()
                .PerformRegistration(username, email, password, haveInviteCode ? inviteCode : null);

            if (result == RegistrationResult.AlreadyRegistered)
                return;

            if (result == RegistrationResult.EmailAddressInUse)
            {
                InformationToast.Instance.Enqueue("با این ایمیل قبلا یه حساب ثبت شده. نکنه می‌خواستی به حسابت وارد شی؟");
                return;
            }

            if (result == RegistrationResult.InvalidEmailAddress)
            {
                InformationToast.Instance.Enqueue("ایمیلی که وارد کردی صحیح نیست.");
                return;
            }

            if (result == RegistrationResult.PasswordNotComplexEnough)
            {
                InformationToast.Instance.Enqueue("گذرواژه‌ای که انتخاب کردی به اندازه کافی طولانی نیست.");
                return;
            }

            if (result == RegistrationResult.UsernameInUse)
            {
                InformationToast.Instance.Enqueue("این نام کاربری رو یکی قبلا انتخاب کرده.");
                return;
            }

            if (result == RegistrationResult.InvalidInviteCode)
            {
                InformationToast.Instance.Enqueue("کد دعوتی که وارد کردی درست نیست.");
                return;
            }

            var td = TransientData.Instance;

            using (ChangeNotifier.BeginBatch())
            {
                td.UserName.Value = username;
                td.EmailAddress.Value = email;
                td.RegistrationStatus.Value = RegistrationStatus.EmailAndPassword;

                if (haveInviteCode)
                {
                    GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "gold", totalGold - td.Gold.Value, "gift", "invitee");
                    SoundEffectManager.Play(SoundEffect.GainCoins);
                    td.Gold.Value = totalGold;
                }
            }
        }

        SettingsMenu.Instance.HideSubMenus();

        if (haveInviteCode)
            InformationToast.Instance.Enqueue($"تبریک، حسابت ساخته شد و از دوستت {TransientData.Instance.ConfigValues.InviteeReward}‌ سکه جایزه گرفتی!");
        else
            InformationToast.Instance.Enqueue("تبریک، حسابت ساخته شد!");
    });
}
