using GameAnalyticsSDK;
using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundWinRewardUI : MonoBehaviour
{
    bool timerActive;
    float lastRefreshTime;

    Image progressFill;
    GameObject progressWaiting, giftWaiting, giftNormal, giftReady;
    TextMeshProUGUI rewardText, statusText;
    RoundWinRewardExplanationBox explanationBox;

    void Start()
    {
        TransientData td = TransientData.Instance;

        MenuManager.Instance.Menu<MainMenu>().Refresh += Refresh;
        td.CurrentNumRoundsWonForReward.ValueChanged += CurrentNumRoundsWonForReward_ValueChanged;
        td.NextRoundWinRewardTime.ValueChanged += NextRoundWinRewardTime_ValueChanged;

        progressFill = transform.Find("Progress/Fill").GetComponent<Image>();
        progressWaiting = transform.Find("Progress/Waiting").gameObject;
        giftWaiting = transform.Find("GiftWaiting").gameObject;
        giftNormal = transform.Find("GiftNormal").gameObject;
        giftReady = transform.Find("GiftReady").gameObject;
        rewardText = transform.Find("RewardText").GetComponent<TextMeshProUGUI>();
        statusText = transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
        explanationBox = transform.Find("ExplanationBox").GetComponent<RoundWinRewardExplanationBox>();

        Refresh();
    }

    void OnDestroy()
    {
        TransientData td = TransientData.Instance;

        td.NextRoundWinRewardTime.ValueChanged -= NextRoundWinRewardTime_ValueChanged;
        td.CurrentNumRoundsWonForReward.ValueChanged -= CurrentNumRoundsWonForReward_ValueChanged;
        MenuManager.Instance?.Menu<MainMenu>().ApplyIfNotNull(m => m.Refresh -= Refresh);
    }

    void NextRoundWinRewardTime_ValueChanged(DateTime newValue) => Refresh();

    void CurrentNumRoundsWonForReward_ValueChanged(uint newValue) => Refresh();

    void Refresh() => Refresh(true);

    void Refresh(bool force)
    {
        if (!force && Time.time - 0.1f < lastRefreshTime)
            return;

        var td = TransientData.Instance;

        progressFill.gameObject.SetActive(false);
        progressWaiting.SetActive(false);
        giftWaiting.SetActive(false);
        giftNormal.SetActive(false);
        giftReady.SetActive(false);
        rewardText.gameObject.SetActive(false);
        statusText.gameObject.SetActive(false);

        if (td.NextRoundWinRewardTime > DateTime.Now)
        {
            timerActive = true;

            var remaining = td.NextRoundWinRewardTime - DateTime.Now;

            progressWaiting.SetActive(true);
            giftWaiting.SetActive(true);
            statusText.gameObject.SetActive(true);
            Translation.SetTextNoShape(statusText, PersianTextShaper.PersianTextShaper.ShapeText($"{(int)remaining.TotalHours}:{remaining.Minutes:00}:{remaining.Seconds:00}"));
        }
        else if (td.CurrentNumRoundsWonForReward >= td.ConfigValues.NumRoundsToWinToGetReward)
        {
            timerActive = false;

            progressFill.gameObject.SetActive(true);
            progressFill.fillAmount = 1.0f;
            giftNormal.SetActive(true);
            giftReady.SetActive(true);
            rewardText.gameObject.SetActive(true);
            Translation.SetTextNoShape(rewardText, $"+{PersianTextShaper.PersianTextShaper.ShapeText(td.ConfigValues.NumGoldRewardForWinningRounds.ToString())}{MoneySprites.SingleCoin}");
        }
        else
        {
            timerActive = false;

            giftNormal.SetActive(true);
            progressFill.gameObject.SetActive(true);
            progressFill.fillAmount = td.CurrentNumRoundsWonForReward / (float)td.ConfigValues.NumRoundsToWinToGetReward * 0.9f;
            statusText.gameObject.SetActive(true);
            Translation.SetTextNoShape(statusText, PersianTextShaper.PersianTextShaper.ShapeText($"{td.CurrentNumRoundsWonForReward} / {td.ConfigValues.NumRoundsToWinToGetReward}"));
        }

        lastRefreshTime = Time.time;
    }

    void Update()
    {
        if (timerActive)
            Refresh(false);
    }

    public void TakeRewardForWinningRounds() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var td = TransientData.Instance;

        if (td.NextRoundWinRewardTime > DateTime.Now)
        {
            explanationBox.Show($"جعبه جایزه هر {TransientData.Instance.ConfigValues.RoundWinRewardInterval.FormatAsPersianExpression()} یک بار فعال می‌شه.");
            return;
        }

        if (td.CurrentNumRoundsWonForReward < td.ConfigValues.NumRoundsToWinToGetReward)
        {
            explanationBox.Show($"هر وقت {TransientData.Instance.ConfigValues.NumRoundsToWinToGetReward} دست ببری، جعبه جایزه شارژ می‌شه و می‌تونی جایزه‌تو بگیری!");
            return;
        }

        using (LoadingIndicator.Show(true))
        {
            var (gold, timeRemaining) = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().TakeRewardForWinningRounds();

            using (ChangeNotifier.BeginBatch(true))
            {
                td.CurrentNumRoundsWonForReward.Value = 0;
                td.NextRoundWinRewardTime.Value = DateTime.Now + timeRemaining;
                GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "gold", gold - td.Gold.Value, "reward", "round win");
            }
            td.Gold.Value = gold;
            SoundEffectManager.Play(SoundEffect.GainCoins);
        }

        Refresh();
    });
}
