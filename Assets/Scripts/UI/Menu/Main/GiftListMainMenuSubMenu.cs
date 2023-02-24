using GameAnalyticsSDK;
using Network;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiftListMainMenuSubMenu : MainMenuSubMenu
{
    [SerializeField] Color adButtonDisabledColor = default;

    Button adButton;

    protected override void Awake()
    {
        base.Awake();

        adButton = transform.Find("VideoAd").GetComponent<Button>();
    }

    void Start()
    {
        var td = TransientData.Instance;

        Translation.SetTextNoTranslate(adButton.transform.Find("Text").GetComponent<TextMeshProUGUI>(), $"دیدن ویدیو ({td.ConfigValues.VideoAdGold})");
        Translation.SetTextNoTranslate(transform.Find("InviteFriend/Text").GetComponent<TextMeshProUGUI>(), $"دعوت دوستان ({td.ConfigValues.InviterReward})");
    }

    void Update() => Refresh();

    public override void Show()
    {
        base.Show();
        Refresh();
    }

    void Refresh() =>
        adButton.colors = 
            adButton.colors.Apply(c => c.normalColor = TransientData.Instance.CoinRewardVideoTracker.Value.CanWatchNow() ? Color.white : adButtonDisabledColor);

    public void InviteFriendTapped() => MenuManager.Instance.Menu<MainMenu>().ShowSubMenu<InviteFriendMainMenuSubMenu>();

    public void ShowAd() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var td = TransientData.Instance;

        var ar = AdRepository.Instance;
        if (!ar.IsAdAvailable(AdRepository.AdZone.CoinReward, true))
        {
            var (mustWait, remainingTime) = td.CoinRewardVideoTracker.Value.GetUnavailableReason();

            if (mustWait)
                DialogBox.Instance.Show($"برای گرفتن هدیه بعدی، باید تا فردا صبر کنی.", "باشه").Ignore();
            else if (remainingTime.HasValue)
                DialogBox.Instance.Show($"برای گرفتن هدیه بعدی، باید {remainingTime.Value.FormatAsPersianExpression(excludeSeconds: true)} صبر کنی.", "باشه").Ignore();
            else
                DialogBox.Instance.Show($"هنوز فیلم آماده نشده، بعدا دوباره امتحان کن.", "باشه").Ignore();

            return;
        }

        var ep = ConnectionManager.Instance.EndPoint<SystemEndPoint>();

        using (LoadingIndicator.Show(true))
        {
            if (await ar.StartRewardedVideoAd(AdRepository.AdZone.CoinReward))
            {
                var gold = await ep.GiveVideoAdReward();

                GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "gold", gold - td.Gold, "reward", "video_ad");

                td.Gold.Value = gold;
                SoundEffectManager.Play(SoundEffect.GainCoins);
            }
        }

        MenuManager.Instance.Menu<MainMenu>().HideSubMenus();

        if (!td.CoinRewardVideoNotificationsEnabled.Value.HasValue)
        {
            var notificationsEnabled = await DialogBox.Instance.Show("می‌خوای هر وقت هدیه بعدی آماده بود بهت خبر بدیم؟", "آره", "نه") == DialogBox.Result.Yes;
            ep.SetCoinRewardVideoNotificationsEnabled(notificationsEnabled);
            td.CoinRewardVideoNotificationsEnabled.Value = notificationsEnabled;
        }
    });
}
