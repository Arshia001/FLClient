using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnlimitedPlayButton : SingletonBehaviour<UnlimitedPlayButton>
{
    TextMeshProUGUI statusText;

    protected override void Awake()
    {
        base.Awake();

        statusText = transform.Find("StatusText").GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        var conf = TransientData.Instance.ConfigValues;
        Translation.SetTextNoTranslate(transform.Find("Text").GetComponent<TextMeshProUGUI>(), $"{conf.MaxActiveGamesWhenUpgraded - conf.MaxActiveGames} بازی بیش‌تر");
    }

    void Update()
    {
        TransientData td = TransientData.Instance;

        var endTime = td.UpgradedActiveGameLimitEndTime.Value;

        statusText.isRightToLeftText = true;
        if (endTime.HasValue && endTime.Value > DateTime.Now)
            Translation.SetTextNoShape(statusText, PersianTextShaper.PersianTextShaper.ShapeText(GetTimeSpanString(endTime.Value - DateTime.Now), rightToLeftRenderDirection: true));
        else
            Translation.SetTextNoShape(statusText, PersianTextShaper.PersianTextShaper.ShapeText(td.ConfigValues.UpgradedActiveGameLimitPrice.ToString(), rightToLeftRenderDirection: true) + "     " + MoneySprites.CoinStack);
    }

    string GetTimeSpanString(TimeSpan timeSpan) => $"{(int)timeSpan.TotalHours}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
}
