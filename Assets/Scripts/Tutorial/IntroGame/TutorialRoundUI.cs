using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class TutorialRoundUI : MonoBehaviour
{
    RectTransform timeBar;
    Rect timeBarInitRect;

    TextMeshProUGUI timeText;

    TextMeshProUGUI categoryNameText;

    ScrollRect answerListScrollRect;
    Transform wordsContainer;
    GameObject wordTemplate;

    public TimeSpan RemainingTime { get; private set; }
    TimeSpan roundTotalTime;

    public ImmInputField InputField { get; private set; }

    Image extendTimeButtonImage;
    Image revealWordButtonImage;

    AudioSource audioSource;

    public bool CanUseTimePowerup { get; set; } = false;

    public bool CanUseWordPowerup { get; set; } = false;

    // The "round ending" audio clip doesn't loop, be careful not to stop time when it's playing
    public bool TimePaused { get; set; } = false;

    void Awake()
    {
        timeBar = transform.Find("TimeBar") as RectTransform;

        timeText = transform.Find("TimeText").GetComponent<TextMeshProUGUI>();

        categoryNameText = transform.Find("AnswersContainer/Category/Text").GetComponent<TextMeshProUGUI>();

        answerListScrollRect = transform.Find("AnswersContainer/AnswersList").GetComponent<ScrollRect>();
        wordsContainer = transform.Find("AnswersContainer/AnswersList/Viewport/Content");
        wordTemplate = wordsContainer.Find("Template").gameObject;

        InputField = transform.Find("Answer").GetComponent<ImmInputField>();
        InputField.onSubmit.AddListener(InputField_Submit);

        var td = TransientData.Instance;
        Translation.SetTextNoShape(transform.Find("ExtraTimeButton/PriceText").GetComponent<TextMeshProUGUI>(),
            PersianTextShaper.PersianTextShaper.ShapeText(td.ConfigValues.RoundTimeExtensionPrices[0].ToString()) + " " + MoneySprites.SingleCoin);
        Translation.SetTextNoShape(transform.Find("HelpButton/PriceText").GetComponent<TextMeshProUGUI>(),
            PersianTextShaper.PersianTextShaper.ShapeText(td.ConfigValues.RevealWordPrices[0].ToString()) + " " + MoneySprites.SingleCoin);

        extendTimeButtonImage = transform.Find("ExtraTimeButton").GetComponent<Image>();
        revealWordButtonImage = transform.Find("HelpButton").GetComponent<Image>();

        audioSource = GetComponent<AudioSource>();
        audioSource.Stop();

        Hide();
    }

    private void InputField_Submit(string _text) => SubmitWord();

    public void SubmitWord()
    {
        var text = InputField.text;
        if (!string.IsNullOrWhiteSpace(text))
        TutorialManager.Instance.PlayWord(text);
        InputField.text = "";
    }

    public void AddWord(string word, int score, bool duplicate)
    {
        if (isActiveAndEnabled)
            StartCoroutine(AddWord_Impl(word, score, duplicate));
    }

    IEnumerator AddWord_Impl(string word, int score, bool duplicate)
    {
        var go = Instantiate(wordTemplate);
        go.SetActive(true);

        var tr = go.transform;
        tr.SetParent(wordsContainer, false);

        var wordText = tr.Find("Word").GetComponent<TextMeshProUGUI>();
        Translation.SetTextNoTranslate(wordText, word);
        wordText.fontStyle = duplicate ? FontStyles.Strikethrough : FontStyles.Normal;

        var scoreTr = tr.Find(score.ToString());
        if (scoreTr)
            scoreTr.gameObject.SetActive(true);

        SoundEffectManager.Play(
            score == 1 ? SoundEffect.Word_1Score :
            score == 2 ? SoundEffect.Word_2Score :
            score == 3 ? SoundEffect.Word_3Score :
            SoundEffect.Word_0Score
            );

        yield return null;

        answerListScrollRect.verticalNormalizedPosition = 0;
    }

    public void StartRound(string category, TimeSpan roundTime)
    {
        gameObject.SetActive(true);

        RemainingTime = roundTotalTime = roundTime;

        Translation.SetTextNoTranslate(categoryNameText, category);

        wordsContainer.ClearContainer();

        Canvas.ForceUpdateCanvases();

        UpdateTimers();
    }

    bool CanUsePowerUp(PowerUpType type)
    {
        if (type == PowerUpType.ExtendRoundTime)
            return CanUseTimePowerup;
        else if (type == PowerUpType.RevealWord)
            return CanUseWordPowerup;
        return false;
    }

    public void Hide() => gameObject.SetActive(false);

    void Update()
    {
        if (!TimePaused)
        {
            RemainingTime -= TimeSpan.FromSeconds(Time.deltaTime);
            if (RemainingTime < TimeSpan.Zero)
                RemainingTime = TimeSpan.Zero;
        }

        if (isActiveAndEnabled && !audioSource.isPlaying && audioSource.clip.length >= RemainingTime.TotalSeconds)
        {
            audioSource.time = 0;
            audioSource.Play();
        }

        if (audioSource.isPlaying && audioSource.clip.length < RemainingTime.TotalSeconds) // In case time extension is used while the clip is playing
            audioSource.Stop();

        UpdateTimers();

        extendTimeButtonImage.color = CanUsePowerUp(PowerUpType.ExtendRoundTime) ? Color.white : new Color(1, 1, 1, 0.66f);
        revealWordButtonImage.color = CanUsePowerUp(PowerUpType.RevealWord) ? Color.white : new Color(1, 1, 1, 0.66f);
    }

    public void ExtendRemainingRoundTime(TimeSpan time)
    {
        SoundEffectManager.Play(SoundEffect.ExtendTimePowerup);

        RemainingTime += time;
    }

    void UpdateTimers()
    {
        if (timeBarInitRect.width <= 0)
            timeBarInitRect = timeBar.rect;

        var remainingMilliseconds = RemainingTime.TotalMilliseconds;
        timeBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, timeBarInitRect.width * Mathf.Clamp01((float)remainingMilliseconds / (float)roundTotalTime.TotalMilliseconds));
        Translation.SetTextNoShape(timeText, ((int)(remainingMilliseconds / 1000)).ToString());
    }

    public void UseRevealWordPowerup()
    {
        if (CanUsePowerUp(PowerUpType.RevealWord))
            TutorialManager.Instance.UseWordPowerup();
    }

    public void ForceFinishGameTime()
    {
        RemainingTime = TimeSpan.Zero;
    }
}
