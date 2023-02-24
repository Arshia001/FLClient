using System.Collections;
using System.Collections.Generic;
using Network;
using UnityEngine;

public class EditBazaarAccountSettingsSubMenu : SettingsSubMenu
{
    ImmInputField usernameText;

    GameObject inviteCode;
    ImmInputField inviteCodeText;

    string originalUsername;

    protected override void Awake()
    {
        base.Awake();

        usernameText = transform.Find("UserName/Input").GetComponent<ImmInputField>();

        inviteCode = transform.Find("InviteCode").gameObject;
        inviteCodeText = inviteCode.transform.Find("Input").GetComponent<ImmInputField>();
    }

    public override void Show()
    {
        base.Show();

        var td = TransientData.Instance;
        originalUsername = td.UserName;

        usernameText.text = originalUsername;

        inviteCode.SetActive(!td.InviteCodeEntered);
        inviteCodeText.text = "";
    }

    public void OK() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var username = usernameText.text;
        var gold = default(ulong?);

        var ep = ConnectionManager.Instance.EndPoint<SystemEndPoint>();

        if (!string.IsNullOrEmpty(inviteCodeText.text))
            using (LoadingIndicator.Show(true))
            {
                gold = await ep.RegisterInviteCode(inviteCodeText.text.ToUpperInvariant());
                if (gold == null)
                {
                    InformationToast.Instance.Enqueue("کد دعوتی که وارد کردی صحیح نیست.");
                    return;
                }
            }

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

        if (gold.HasValue)
        {
            td.Gold.Value = gold.Value;
            td.InviteCodeEntered.Value = true;
            SoundEffectManager.Play(SoundEffect.GainCoins);
            InformationToast.Instance.Enqueue($"تبریک، از دوستت {TransientData.Instance.ConfigValues.InviteeReward}‌ سکه جایزه گرفتی!");
        }

        SettingsMenu.Instance.HideSubMenus();
    });

    public void Logout() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        if (await DialogBox.Instance.Show("مطمئنی می‌خوای از حسابت خارج شی؟", "آره", "نه") == DialogBox.Result.Yes)
            ProfileHelper.LogOut();
    });
}
