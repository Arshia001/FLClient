using Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfigurationSettingsSubMenu : SettingsSubMenu
{
    GameObject soundDisabled, notificationDisabled, coinRewardVideoNotificationDisabled;

    protected override void Awake()
    {
        base.Awake();

        soundDisabled = transform.Find("SoundEffects/Disabled").gameObject;
        notificationDisabled = transform.Find("Notifications/Disabled").gameObject;
        coinRewardVideoNotificationDisabled = transform.Find("CoinRewardVideoNotifications/Disabled").gameObject;

        RefreshSoundImage();
        RefreshNotificationImage();
        RefreshCoinRewardVideoNotificationImage();
    }

    void RefreshSoundImage() => soundDisabled.SetActive(!DataStore.Instance.SoundEnabled);

    void RefreshNotificationImage() => notificationDisabled.SetActive(!TransientData.Instance.NotificationsEnabled);

    void RefreshCoinRewardVideoNotificationImage() => coinRewardVideoNotificationDisabled.SetActive(TransientData.Instance.CoinRewardVideoNotificationsEnabled.Value != true);

    public void FlipSound()
    {
        var v = DataStore.Instance.SoundEnabled;
        v.Value = !v;
        RefreshSoundImage();
    }

    public void FlipNotification()
    {
        var v = TransientData.Instance.NotificationsEnabled;
        v.Value = !v;
        ConnectionManager.Instance.EndPoint<SystemEndPoint>().SetNotificationsEnabled(v);
        RefreshNotificationImage();
    }

    public void FlipCoinRewardVideoNotification()
    {
        var v = TransientData.Instance.CoinRewardVideoNotificationsEnabled;
        v.Value = !(v.Value == true);
        ConnectionManager.Instance.EndPoint<SystemEndPoint>().SetCoinRewardVideoNotificationsEnabled(v.Value.Value);
        RefreshCoinRewardVideoNotificationImage();
    }

    public void ShowCredits() => SettingsMenu.Instance.Show<CreditsSettingsSubMenu>();
}
