using FLGameLogic;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialRoundDetailsUI : MonoBehaviour
{
    TextMeshProUGUI subjectText;

    TextMeshProUGUI myScore;
    TextMeshProUGUI theirScore;

    GameObject playerAnswers, allAnswers;

    Transform myAnswersContainer, theirAnswersContainer;
    GameObject myAnswersTemplate, theirAnswersTemplate;
    TextMeshProUGUI showAllAnswersText, showAllAnswersPriceText;

    Transform allAnswersContainer;
    GameObject allAnswersTemplate;

    GameObject votePending;
    GameObject voteDone;

    bool showingAnswers;

    RoundDisplayInfo round;

    IEnumerable<string> allAnswersCache;

    void Awake()
    {
        subjectText = transform.Find("Subject/Text").GetComponent<TextMeshProUGUI>();

        myScore = transform.Find("Score/MyScore").GetComponent<TextMeshProUGUI>();
        theirScore = transform.Find("Score/TheirScore").GetComponent<TextMeshProUGUI>();

        playerAnswers = transform.Find("AnswersList").gameObject;
        allAnswers = transform.Find("AllAnswersList").gameObject;

        myAnswersContainer = transform.Find("AnswersList/Viewport/Content/Me");
        myAnswersTemplate = myAnswersContainer.Find("Template").gameObject;

        theirAnswersContainer = transform.Find("AnswersList/Viewport/Content/Them");
        theirAnswersTemplate = theirAnswersContainer.Find("Template").gameObject;

        allAnswersContainer = allAnswers.transform.Find("Viewport/Content");
        allAnswersTemplate = allAnswersContainer.Find("Template").gameObject;

        showAllAnswersText = transform.Find("ShowAllAnswersButton/Text").GetComponent<TextMeshProUGUI>();
        showAllAnswersPriceText = transform.Find("ShowAllAnswersButton/PriceText").GetComponent<TextMeshProUGUI>();

        votePending = transform.Find("VotePending").gameObject;
        voteDone = transform.Find("VoteDone").gameObject;
    }

    public void Show(RoundDisplayInfo round)
    {
        allAnswersCache = null;

        gameObject.SetActive(true);

        this.round = round;

        Translation.SetTextNoTranslate(subjectText, round.category);

        Translation.SetTextNoTranslate(myScore, round.myScore?.ToString() ?? "؟");
        Translation.SetTextNoTranslate(theirScore, round.theirScore?.ToString() ?? "؟");

        SetLikeButtonsVisible(!round.roundRated);

        ShowPlayerAnswers();
    }

    private void ShowPlayerAnswers()
    {
        showingAnswers = false;
        SetShowAnswerButtonText(showingAnswers, round.haveAnswers || allAnswersCache != null);

        playerAnswers.SetActive(true);
        allAnswers.SetActive(false);

        myAnswersContainer.ClearContainer();
        theirAnswersContainer.ClearContainer();

        foreach (var answer in round.myWords)
            InitializeWordEntry(myAnswersContainer.AddListItem(myAnswersTemplate), answer);

        foreach (var answer in round.theirWords)
            InitializeWordEntry(theirAnswersContainer.AddListItem(theirAnswersTemplate), answer);
    }

    public void ShowAllAnswers(IEnumerable<string> answers)
    {
        showingAnswers = true;
        SetShowAnswerButtonText(showingAnswers, round.haveAnswers || allAnswersCache != null);

        allAnswers.SetActive(true);
        playerAnswers.SetActive(false);

        allAnswersContainer.ClearContainer();

        foreach (var answer in answers)
            Translation.SetTextNoTranslate(allAnswersContainer.AddListItem(allAnswersTemplate).Find("Word").GetComponent<TextMeshProUGUI>(), answer);
    }

    void InitializeWordEntry(Transform tr, WordScorePair wordScore)
    {
        Translation.SetTextNoTranslate(tr.Find("Word").GetComponent<TextMeshProUGUI>(), wordScore.word);
        tr.Find(wordScore.score.ToString()).gameObject.SetActive(true);
    }

    void SetShowAnswerButtonText(bool showingAnswers, bool haveAnswers)
    {
        showAllAnswersText.alignment = showingAnswers || haveAnswers ? TextAlignmentOptions.Center : TextAlignmentOptions.Right;
        Translation.SetText(showAllAnswersText, showingAnswers ? "ShowOwnAnswers" : "ShowAllAnswers");

        showAllAnswersPriceText.gameObject.SetActive(!showingAnswers && !haveAnswers);
        Translation.SetTextNoShape(showAllAnswersPriceText, MoneySprites.SingleCoin + " " +
            PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.ConfigValues.GetAnswersPrice.ToString()));
    }

    public void SetLikeButtonsVisible(bool visible)
    {
        votePending.SetActive(visible);
        voteDone.SetActive(!visible);
    }

    public void Hide() => gameObject.SetActive(false);
}
