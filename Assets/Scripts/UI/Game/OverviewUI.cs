using FLGameLogic;
using Network;
using Network.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum RoundState
{
    WaitingForThem,
    WaitingForMe,
    Complete
}

public class RoundDisplayInfo
{
    public FullGameInfo gameInfo;
    public int index;
    public RoundState state;
    public string category;
    public uint? myScore;
    public uint? theirScore;
    public IEnumerable<WordScorePair> myWords;
    public IEnumerable<WordScorePair> theirWords;
    public bool roundRated;
    public bool haveAnswers;
}

public class OverviewUI : MonoBehaviour, IGameMenu
{
    [SerializeField] Color winScoreColor = default, loseScoreColor = default, drawScoreColor = default;

    TextMeshProUGUI myNameText;
    TextMeshProUGUI myLevelText;
    TextMeshProUGUI myScoreText;
    AvatarDisplay myAvatar;
    TextMeshProUGUI theirNameText;
    TextMeshProUGUI theirLevelText;
    TextMeshProUGUI theirScoreText;
    AvatarDisplay theirAvatar;
    Image theirAvatarPlaceHolder;

    Transform roundsContainer;
    GameObject roundTemplate;

    GameObject playButton;
    GameObject waitingForOpponentText;
    GameObject gameExpiredForMeText;
    GameObject gameExpiredForThemText;
    GameObject gameFinishedText;

    RoundDetailsUI roundDetails;
    SuggestWordsUI suggestWords;

    List<RoundDisplayInfo> rounds;

    public bool IsVisible => gameObject.activeInHierarchy;

    public RectTransform WaitingForOpponentTextTransform => waitingForOpponentText.transform as RectTransform;
    public RectTransform BackButtonTransform { get; private set; }

    public RoundDetailsUI RoundDetails => roundDetails;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        GameManager.Instance.BackToMenu();
        return true;
    });

    void Awake()
    {
        myNameText = transform.Find("MyProfile/Name").GetComponent<TextMeshProUGUI>();
        myLevelText = transform.Find("MyProfile/Level/Text").GetComponent<TextMeshProUGUI>();
        myAvatar = transform.Find("MyProfile/Avatar").GetComponent<AvatarDisplay>();
        theirNameText = transform.Find("TheirProfile/Name").GetComponent<TextMeshProUGUI>();
        theirLevelText = transform.Find("TheirProfile/Level/Text").GetComponent<TextMeshProUGUI>();
        theirAvatar = transform.Find("TheirProfile/Avatar").GetComponent<AvatarDisplay>();
        theirAvatarPlaceHolder = transform.Find("TheirProfile/AvatarPlaceHolder").GetComponent<Image>();

        myScoreText = transform.Find("Score/MyScore").GetComponent<TextMeshProUGUI>();
        theirScoreText = transform.Find("Score/TheirScore").GetComponent<TextMeshProUGUI>();

        roundsContainer = transform.Find("Rounds");
        roundTemplate = roundsContainer.Find("Template").gameObject;

        playButton = transform.Find("PlayButton").gameObject;
        waitingForOpponentText = transform.Find("WaitingForOpponentText").gameObject;
        gameFinishedText = transform.Find("GameFinishedText").gameObject;
        gameExpiredForMeText = transform.Find("GameExpiredForMeText").gameObject;
        gameExpiredForThemText = transform.Find("GameExpiredForThemText").gameObject;

        BackButtonTransform = transform.Find("ActionBar/BackButton") as RectTransform;

        roundDetails = transform.Find("RoundDetails").GetComponent<RoundDetailsUI>();

        suggestWords = transform.Find("SuggestWords").GetComponent<SuggestWordsUI>();
    }

    void Start()
    {
        roundDetails.Hide();
        suggestWords.Hide();
    }

    public void ShowGame(string theirName, AvatarDTO theirAvatar, uint theirLevel, uint myScore, uint theirScore,
        IEnumerable<RoundDisplayInfo> rounds, bool gameFinished, bool gameExpired, bool expiredForMe)
    {
        gameObject.SetActive(true);

        backStackHandler.MenuShown();

        var td = TransientData.Instance;
        Translation.SetTextNoTranslate(myNameText, td.UserName);
        Translation.SetTextNoTranslate(myLevelText, td.Level.ToString());
        Translation.SetTextNoTranslate(myScoreText, myScore.ToString());
        myAvatar.SetAvatar(td.Avatar);

        Translation.SetTextNoTranslate(theirNameText, theirName);
        Translation.SetTextNoTranslate(theirLevelText, theirLevel.ToString());
        Translation.SetTextNoTranslate(theirScoreText, theirScore.ToString());
        this.theirAvatar.SetAvatar(theirAvatar);
        theirAvatarPlaceHolder.gameObject.SetActive(theirAvatar == null);

        roundsContainer.ClearContainer();

        this.rounds = rounds.ToList();

        var lastRound = default(RoundDisplayInfo);
        foreach (var round in rounds)
        {
            var tr = roundsContainer.AddListItem(roundTemplate);

            bool? isMyWin = round.myScore.HasValue && round.theirScore.HasValue ?
                (round.myScore > round.theirScore ? true : round.myScore < round.theirScore ? false : default(bool?))
                : null;

            tr.Find("MyScore").GetComponent<Image>().color = isMyWin == null ? drawScoreColor : isMyWin.Value ? winScoreColor : loseScoreColor;
            tr.Find("TheirScore").GetComponent<Image>().color = isMyWin == null ? drawScoreColor : isMyWin.Value ? loseScoreColor : winScoreColor;

            Translation.SetTextNoTranslate(tr.Find("MyScore/Text").GetComponent<TextMeshProUGUI>(), round.myScore?.ToString() ?? "؟");
            Translation.SetTextNoTranslate(tr.Find("TheirScore/Text").GetComponent<TextMeshProUGUI>(), round.theirScore?.ToString() ?? "؟");
            Translation.SetTextNoTranslate(tr.Find("Subject/Text").GetComponent<TextMeshProUGUI>(), round.category ?? "؟؟؟");

            if (round.state != RoundState.WaitingForMe)
            {
                tr.GetComponent<Button>().onClick.AddListener(new UnityAction(() => ShowRoundDetails(round.index)));
            }
            else
            {
                tr.GetComponent<Button>().interactable = false;
            }

            lastRound = round;
        }

        playButton.SetActive(false);
        waitingForOpponentText.SetActive(false);
        gameFinishedText.SetActive(false);
        gameExpiredForMeText.SetActive(false);
        gameExpiredForThemText.SetActive(false);

        if (gameExpired)
        {
            if (expiredForMe)
                gameExpiredForMeText.SetActive(true);
            else
                gameExpiredForThemText.SetActive(true);
        }
        else if (gameFinished)
            gameFinishedText.SetActive(true);
        else if (lastRound != null)
        {
            if (lastRound.state == RoundState.WaitingForMe)
                playButton.SetActive(true);
            else if (lastRound.state == RoundState.WaitingForThem)
                waitingForOpponentText.SetActive(true);
        }
    }

    private void ShowRoundDetails(RoundDisplayInfo round) => roundDetails.Show(round);

    public void ShowRoundDetails(int roundIndex)
    {
        if (rounds != null && rounds.Count > roundIndex)
        {
            ShowRoundDetails(rounds[roundIndex]);
            if (rounds[roundIndex].state == RoundState.WaitingForThem)
                TutorialManager.Instance.BackToOverviewOnOpponentsTurn();
        }
    }

    public Task Hide()
    {
        backStackHandler.MenuHidden();

        roundDetails.Hide();
        suggestWords.Hide();

        gameObject.SetActive(false);

        return Task.CompletedTask;
    }

    public void Show() => throw new NotSupportedException();

    public void ShowSuggestWords(RoundDisplayInfo round) =>
        suggestWords.Show(
            round.category,
            round.myWords
                .Where(w => w.score == 0)
                .Select(w => w.word)
            );

    public void ShowRules() => transform.Find("Rules").GetComponent<RulesListing>().Show();

    public void NotifyOpponentRound() => InformationToast.Instance.Enqueue("باید صبر کنی تا حریفت نوبتشو بازی کنه.");
}
