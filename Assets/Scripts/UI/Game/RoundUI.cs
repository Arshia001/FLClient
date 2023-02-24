using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameAnalyticsSDK;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class RoundUI : MonoBehaviour, IGameMenu
{
    RectTransform timeBar;
    Rect timeBarInitRect;

    TextMeshProUGUI timeText;

    TextMeshProUGUI categoryNameText;

    ScrollRect answerListScrollRect;
    Transform wordsContainer;
    GameObject wordTemplate;

    DateTime roundEndTime;
    TimeSpan roundTotalTime;

    ImmInputField inputField;

    Image extendTimeButtonImage;
    Image revealWordButtonImage;

    int numTimeExtensions;
    int numWordsRevealed;
    bool communicatingWithServer;

    AudioSource audioSource;
    
    readonly MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() => false);

    TextMeshProUGUI ExtendTimePriceText;
    TextMeshProUGUI RevealWordPriceText;

    void Awake()
    {
        timeBar = transform.Find("TimeBar") as RectTransform;

        timeText = transform.Find("TimeText").GetComponent<TextMeshProUGUI>();

        categoryNameText = transform.Find("AnswersContainer/Category/Text").GetComponent<TextMeshProUGUI>();

        answerListScrollRect = transform.Find("AnswersContainer/AnswersList").GetComponent<ScrollRect>();
        wordsContainer = transform.Find("AnswersContainer/AnswersList/Viewport/Content");
        wordTemplate = wordsContainer.Find("Template").gameObject;

        inputField = transform.Find("Answer").GetComponent<ImmInputField>();
        inputField.onSubmit.AddListener(InputField_Submit);

        ExtendTimePriceText = transform.Find("ExtraTimeButton/PriceText").GetComponent<TextMeshProUGUI>();
        RevealWordPriceText = transform.Find("HelpButton/PriceText").GetComponent<TextMeshProUGUI>();

        extendTimeButtonImage = transform.Find("ExtraTimeButton").GetComponent<Image>();
        revealWordButtonImage = transform.Find("HelpButton").GetComponent<Image>();

        audioSource = GetComponent<AudioSource>();
        audioSource.Stop();
    }

    void InputField_Submit(string _text) => SubmitWord();

    public void SubmitWord() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var text = inputField.text;
        inputField.text = "";

        if (!string.IsNullOrWhiteSpace(text))
        {
            var tr = AddPendingWord(text.Trim());
            var (actual, score, duplicate) = await GameManager.Instance.PlayWord(text);
            SetWordScore(actual, score, duplicate, tr);
        }
    });

    public void RevealWord(string word, int score)
    {
        if (!isActiveAndEnabled)
            return;

        ++numWordsRevealed;

        AddWord(word, score, false);
    }

    public void AddWord(string word, int score, bool duplicate)
    {
        if (isActiveAndEnabled)
        {
            var tr = AddPendingWord(word);
            SetWordScore(word, score, duplicate, tr);
        }
    }

    void SetWordScore(string word, int score, bool duplicate, Transform tr)
    {
        var wordText = tr.Find("Word").GetComponent<TextMeshProUGUI>();
        Translation.SetTextNoTranslate(wordText, word);
        wordText.fontStyle = duplicate ? FontStyles.Strikethrough : FontStyles.Normal;

        tr.Find("Pending").gameObject.SetActive(false);

        var scoreTr = tr.Find(score.ToString());
        if (scoreTr)
            scoreTr.gameObject.SetActive(true);

        SoundEffectManager.Play(
            score == 1 ? SoundEffect.Word_1Score :
            score == 2 ? SoundEffect.Word_2Score :
            score == 3 ? SoundEffect.Word_3Score :
            SoundEffect.Word_0Score
            );
    }

    Transform AddPendingWord(string word)
    {
        var go = Instantiate(wordTemplate);
        go.SetActive(true);

        var tr = go.transform;
        tr.SetParent(wordsContainer, false);

        var wordText = tr.Find("Word").GetComponent<TextMeshProUGUI>();
        Translation.SetTextNoTranslate(wordText, word);

        tr.Find("Pending").gameObject.SetActive(true);

        StartCoroutine(ScrollToBottomAfterOneFrame());

        SoundEffectManager.Play(SoundEffect.Word_Pending);

        return tr;
    }

    IEnumerator ScrollToBottomAfterOneFrame()
    {
        yield return null;

        answerListScrollRect.verticalNormalizedPosition = 0;
    }

    public void StartRound(string category, TimeSpan roundTime)
    {
        gameObject.SetActive(true);

        backStackHandler.MenuShown();

        roundTotalTime = roundTime;
        roundEndTime = DateTime.Now + roundTotalTime;

        Translation.SetTextNoTranslate(categoryNameText, category);

        wordsContainer.ClearContainer();

        Canvas.ForceUpdateCanvases();

        UpdateTimers();

        numTimeExtensions = 0;
        numWordsRevealed = 0;

        RefreshPowerUpPrices();

        inputField.text = "";
        EventSystem.current.SetSelectedGameObject(inputField.gameObject);
    }

    public void ResumeRound(string category, TimeSpan roundTime, IEnumerable<FLGameLogic.WordScorePair> words)
    {
        StartRound(category, roundTime);

        foreach (var ws in words)
            AddWord(ws.word, ws.score, false);
    }

    private void RefreshPowerUpPrices()
    {
        Translation.SetTextNoShape(ExtendTimePriceText, PersianTextShaper.PersianTextShaper.ShapeText(GetPrice(PowerUpType.ExtendRoundTime).ToString()) + " " + MoneySprites.SingleCoin);
        Translation.SetTextNoShape(RevealWordPriceText, PersianTextShaper.PersianTextShaper.ShapeText(GetPrice(PowerUpType.RevealWord).ToString()) + " " + MoneySprites.SingleCoin);
    }

    uint GetPrice(PowerUpType powerUp)
    {
        switch (powerUp)
        {
            case PowerUpType.ExtendRoundTime:
                {
                    var prices = TransientData.Instance.ConfigValues.RoundTimeExtensionPrices;
                    return prices[Mathf.Clamp(GameManager.Instance.GetTimeExtensionsUsed(), 0, prices.Count - 1)];
                }

            case PowerUpType.RevealWord:
                {
                    var prices = TransientData.Instance.ConfigValues.RevealWordPrices;
                    return prices[Mathf.Clamp(numWordsRevealed, 0, prices.Count - 1)];
                }

            default:
                throw new Exception($"Unknown power-up type {powerUp}");
        }
    }

    bool CanUsePowerUp(PowerUpType powerUp, bool showToastIfUnavailable)
    {
        if (communicatingWithServer)
            return false;

        var td = TransientData.Instance;

        var price = GetPrice(powerUp);

        if (td.Gold < price)
        {
            if (showToastIfUnavailable)
                InGameErrorToast.Instance.Enqueue("پول کافی نداری!");
            return false;
        }

        if (powerUp == PowerUpType.ExtendRoundTime && numTimeExtensions >= td.ConfigValues.NumTimeExtensionsPerRound)
        {
            if (showToastIfUnavailable)
                InGameErrorToast.Instance.Enqueue($"تو هر دست فقط {td.ConfigValues.NumTimeExtensionsPerRound} بار می‌تونی زمانتو اضافه کنی!");
            return false;
        }

        return true;
    }

    public void UseIncreaseRoundTimePowerup()
    {
        if (CanUsePowerUp(PowerUpType.ExtendRoundTime, true))
        {
            GameAnalytics.NewDesignEvent("extend time used");
            communicatingWithServer = true;
            GameManager.Instance.IncreaseRoundTime();
        }
    }

    public void UseRevealWordPowerup()
    {
        if (CanUsePowerUp(PowerUpType.RevealWord, true))
        {
            GameAnalytics.NewDesignEvent("reveal word used");
            communicatingWithServer = true;
            GameManager.Instance.RevealWord();
        }
    }

    void Update()
    {
        if (DateTime.Now >= roundEndTime)
        {
            SoundEffectManager.Play(SoundEffect.RoundTimeFinished);
            EndRound();
        }

        var endRoundSoundStartTime = roundEndTime - TimeSpan.FromSeconds(audioSource.clip.length);

        if (isActiveAndEnabled && !audioSource.isPlaying && DateTime.Now >= endRoundSoundStartTime)
        {
            audioSource.time = 0;
            audioSource.Play();
        }

        if (audioSource.isPlaying && DateTime.Now < endRoundSoundStartTime) // In case time extension is used while the clip is playing
            audioSource.Stop();

        UpdateTimers();

        extendTimeButtonImage.color = CanUsePowerUp(PowerUpType.ExtendRoundTime, false) ? Color.white : new Color(1, 1, 1, 0.66f);
        revealWordButtonImage.color = CanUsePowerUp(PowerUpType.RevealWord, false) ? Color.white : new Color(1, 1, 1, 0.66f);
    }

    public void EndRound()
    {
        inputField.ForceDeselect();
        Hide();
        GameManager.Instance.EndRound();
    }

    public void PowerUpRequestComplete()
    {
        communicatingWithServer = false;
        RefreshPowerUpPrices(); 
    }

    public void ExtendRemainingRoundTime(TimeSpan time)
    {
        // In case we ran out of time on this side while waiting for the reply
        if (!isActiveAndEnabled)
            return;

        SoundEffectManager.Play(SoundEffect.ExtendTimePowerup);

        ++numTimeExtensions;
        roundEndTime += time;
    }

    void UpdateTimers()
    {
        if (timeBarInitRect.width <= 0)
            timeBarInitRect = timeBar.rect;

        var remainingMilliseconds = Mathf.Max(0, (float)(roundEndTime - DateTime.Now).TotalMilliseconds);
        timeBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timeBarInitRect.width * Mathf.Clamp01(remainingMilliseconds / (float)roundTotalTime.TotalMilliseconds));
        Translation.SetTextNoShape(timeText, ((int)(remainingMilliseconds / 1000)).ToString());
    }

    public void Show() => throw new NotSupportedException();

    public Task Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);

        return Task.CompletedTask;
    }
}

