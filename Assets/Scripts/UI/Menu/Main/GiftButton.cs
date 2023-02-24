using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GiftButton : MonoBehaviour
{
    GameObject button;
    Animation buttonAnimation;
    TextMeshProUGUI timeText;

    void Awake()
    {
        button = transform.Find("Button").gameObject;
        
        buttonAnimation = button.GetComponent<Animation>();

        timeText = button.transform.Find("WaitTime").GetComponent<TextMeshProUGUI>();
        timeText.gameObject.SetActive(false);
    }

    void Update() => Refresh();

    void OnEnable() => Refresh();

    void Refresh()
    {
        if (TutorialManager.Instance?.TutorialInProgress ?? false)
        {
            button.SetActive(false);
            return;
        }
        else
            button.SetActive(true);

        var available = AdRepository.Instance.IsAdAvailable(AdRepository.AdZone.CoinReward);

        if (available && !buttonAnimation.isPlaying)
            buttonAnimation.Play();
        else if (!available && buttonAnimation.isPlaying)
        {
            buttonAnimation.Stop();
            buttonAnimation[buttonAnimation.clip.name].normalizedTime = 0.0f;
            buttonAnimation.Sample();
        }

        var (_, remainingTime) = TransientData.Instance.CoinRewardVideoTracker.Value.GetUnavailableReason();
        if (!available && remainingTime.HasValue && remainingTime.Value > TimeSpan.Zero)
        {
            timeText.gameObject.SetActive(true);
            Translation.SetTextNoShape(timeText,
                $"{PersianTextShaper.PersianTextShaper.ShapeText(remainingTime.Value.Hours.ToString())}:" +
                $"{PersianTextShaper.PersianTextShaper.ShapeText(remainingTime.Value.Minutes.ToString("00"))}:" +
                $"{PersianTextShaper.PersianTextShaper.ShapeText(remainingTime.Value.Seconds.ToString("00"))}");
        }
        else
            timeText.gameObject.SetActive(false);
    }
}