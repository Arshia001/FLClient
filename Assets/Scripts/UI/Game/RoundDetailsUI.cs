using FLGameLogic;
using GameAnalyticsSDK;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class RoundDetailsUI : MonoBehaviour
{
    TextMeshProUGUI subjectText;

    TextMeshProUGUI myScore;
    TextMeshProUGUI theirScore;

    GameObject playerAnswers, allAnswers;

    Transform myAnswersContainer, theirAnswersContainer;
    GameObject myAnswersTemplate, theirAnswersTemplate;

    Transform allAnswersContainer;
    GameObject allAnswersTemplate;
    GameObject votePending;
    GameObject voteDone;
    GameObject suggestWordsButton;

    ShowAnswersButton showAnswersButton;
    bool showingAnswers;

    RoundDisplayInfo round;

    IEnumerable<string> allAnswersCache;

    MenuBackStackHandler backStackHandler;

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

        showAnswersButton = transform.Find("ShowAllAnswersButton").GetComponent<ShowAnswersButton>();

        votePending = transform.Find("VotePending").gameObject;
        voteDone = transform.Find("VoteDone").gameObject;
        suggestWordsButton = voteDone.transform.Find("SuggestWords").gameObject;

        backStackHandler = new MenuBackStackHandler(() =>
        {
            Hide();
            return true;
        });
    }

    public void Show(RoundDisplayInfo round)
    {
        allAnswersCache = null;

        backStackHandler.MenuShown();

        gameObject.SetActive(true);

        this.round = round;

        Translation.SetTextNoTranslate(subjectText, round.category);

        Translation.SetTextNoTranslate(myScore, round.myScore?.ToString() ?? "؟");
        Translation.SetTextNoTranslate(theirScore, round.theirScore?.ToString() ?? "؟");

        SetLikeButtonsVisible(!round.roundRated);

        ShowPlayerAnswers();
    }

    void ShowPlayerAnswers()
    {
        showingAnswers = false;
        showAnswersButton.SetText(showingAnswers, round.haveAnswers || allAnswersCache != null);

        playerAnswers.SetActive(true);
        allAnswers.SetActive(false);

        myAnswersContainer.ClearContainer();
        theirAnswersContainer.ClearContainer();

        foreach (var answer in round.myWords)
            InitializeWordEntry(myAnswersContainer.AddListItem(myAnswersTemplate), answer);

        foreach (var answer in round.theirWords)
            InitializeWordEntry(theirAnswersContainer.AddListItem(theirAnswersTemplate), answer);
    }

    // When we have the answers, the server will simply return the list without taking any money
    Task<IEnumerable<string>> GetAllAnswersWhenOwned() =>
        GameManager.Instance.GetRoundAnswers(round.gameInfo, round.index, false);

    Task<IEnumerable<string>> GetAllAnswersByMoney()
    {
        var td = TransientData.Instance;
        if (td.Gold < td.ConfigValues.GetAnswersPrice)
        {
            TaskExtensions.RunIgnoreAsync(async () =>
            {
                if (await DialogBox.Instance.Show($"برای دیدن پاسخ‌ها {td.ConfigValues.GetAnswersPrice - td.Gold.Value} سکه کم داری.‌ می‌خوای بریم فروشگاه؟", "بریم", "بی‌خیال") == DialogBox.Result.Yes)
                {
                    Hide();
                    GameManager.Instance.BackToMenu();
                    MenuManager.Instance.ShowShop();
                }
            });
            return Task.FromResult(default(IEnumerable<string>));
        }

        GameAnalytics.NewDesignEvent("AllAnswers:gold");
        GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "gold", td.ConfigValues.GetAnswersPrice, "powerup", "answers");

        return GameManager.Instance.GetRoundAnswers(round.gameInfo, round.index, false);
    }

    async Task<IEnumerable<string>> GetAllAnswersByVideoAd()
    {
        var ar = AdRepository.Instance;

        ar.LogAdClickedToAnalytics(AdRepository.AdZone.GetCategoryAnswers);
        GameAnalytics.NewDesignEvent("AllAnswers:ad");

        if (await ar.StartRewardedVideoAd(AdRepository.AdZone.GetCategoryAnswers))
            return await GameManager.Instance.GetRoundAnswers(round.gameInfo, round.index, true);
        else
            return null;
    }

    void ShowAllAnswers() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        IEnumerable<string> answers;

        if (allAnswersCache != null)
            answers = allAnswersCache;
        else if (round.haveAnswers)
        {
            using (LoadingIndicator.Show(true))
                answers = await GetAllAnswersWhenOwned();
        }
        else if (AdRepository.Instance.IsAdAvailable(AdRepository.AdZone.GetCategoryAnswers))
        {
            var selection = await DialogBox.Instance.Show("می‌خوای با دیدن یه فیلم کوتاه جواب‌ها رو مجانی ببینی؟", "آره", $"پول می‌دم ({TransientData.Instance.ConfigValues.GetAnswersPrice})", true);

            if (selection == DialogBox.Result.Cancel)
                return;

            using (LoadingIndicator.Show(true))
                if (selection == DialogBox.Result.Yes)
                    answers = await GetAllAnswersByVideoAd();
                else
                    answers = await GetAllAnswersByMoney();
        }
        else
        {
            if (await DialogBox.Instance.Show($"می‌خوای {TransientData.Instance.ConfigValues.GetAnswersPrice} سکه بدی تا جواب‌ها رو ببینی؟", "آره", $"نه") == DialogBox.Result.No)
                return;

            using (LoadingIndicator.Show(true))
                answers = await GetAllAnswersByMoney();
        }

        if (answers == null || !answers.Any())
            return;

        round.haveAnswers = true;

        if (allAnswersCache == null)
            allAnswersCache = answers.ToList();

        ShowAllAnswersList(allAnswersCache);
    });

    private void ShowAllAnswersList(IEnumerable<string> answers)
    {
        showingAnswers = true;
        showAnswersButton.SetText(true, true);

        allAnswers.SetActive(true);
        playerAnswers.SetActive(false);

        allAnswersContainer.ClearContainer();

        if (answers.Any())
            foreach (var answer in answers)
                Translation.SetTextNoTranslate(allAnswersContainer.AddListItem(allAnswersTemplate).Find("Word").GetComponent<TextMeshProUGUI>(), answer);
        else
            ShowPlayerAnswers();
    }

    void InitializeWordEntry(Transform tr, WordScorePair wordScore)
    {
        Translation.SetTextNoTranslate(tr.Find("Word").GetComponent<TextMeshProUGUI>(), wordScore.word);
        tr.Find(wordScore.score.ToString()).gameObject.SetActive(true);
    }

    public void Like()
    {
        GameManager.Instance.RateRound(round.gameInfo, round.index, true);
        round.roundRated = true;
        SetLikeButtonsVisible(false);
    }

    public void Dislike()
    {
        GameManager.Instance.RateRound(round.gameInfo, round.index, false);
        round.roundRated = true;
        SetLikeButtonsVisible(false);
    }

    void SetLikeButtonsVisible(bool visible)
    {
        votePending.SetActive(visible);
        voteDone.SetActive(!visible);
        suggestWordsButton.SetActive(round.myWords.Any(w => w.score == 0));
    }

    public void ForceActivateSuggestWordsButton() => suggestWordsButton.SetActive(true);

    public void Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);
    }

    public void ToggleAnswers()
    {
        if (showingAnswers)
            ShowPlayerAnswers();
        else
            ShowAllAnswers();
    }

    public void ShowSuggestWords()
    {
        Hide();
        GameManager.Instance.Menu<OverviewUI>().ShowSuggestWords(round);
    }
}
