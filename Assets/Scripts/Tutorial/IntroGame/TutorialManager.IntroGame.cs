using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameAnalyticsSDK;
using Network;
using Network.Types;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public partial class TutorialManager
{
    [SerializeField] Button startGameButton = default;

    TutorialRoundUI roundUI;

    void StartIntroGame() => StartCoroutine(RunIntroGame());

    IEnumerator RunIntroGame()
    {
        var overviewUI = transform.Find("Game/Overview").GetComponent<TutorialOverviewUI>();
        var groupSelectionUI = transform.Find("Game/GroupSelection").GetComponent<TutorialGroupSelectionUI>();
        roundUI = transform.Find("Game/Round").GetComponent<TutorialRoundUI>();

        var token = StartTutorialSequence();

        #region 1 - Main Menu - tap start game

        GameAnalytics.NewDesignEvent("tutorial:introgame", 1);

        yield return HighlightWithMessageBottomAndWait(startGameButton.transform as RectTransform, TutorialArrow.Direction.Top, "خوش اومدی! این‌جا رو لمس کن بریم تو بازی.", true);

        #endregion

        MenuManager.Instance.HideAll();
        transform.Find("Game").gameObject.SetActive(true);

        #region 2 - Overview - tap start game

        GameAnalytics.NewDesignEvent("tutorial:introgame", 2);

        overviewUI.ShowGame("مداد جنگی", 0, 0, 0,
            new[]
            {
                new RoundDisplayInfo
                {
                    state = RoundState.WaitingForMe
                }
            }, false, false, false);

        yield return HighlightAndWait(overviewUI.transform.Find("PlayButton") as RectTransform, TutorialArrow.Direction.Top);

        overviewUI.Hide();

        #endregion

        #region 3 - Group selection

        GameAnalytics.NewDesignEvent("tutorial:introgame", 3);

        groupSelectionUI.Show(new GroupInfoDTO("جغرافیا", 1), new GroupInfoDTO("اطلاعات عمومی", 2), new GroupInfoDTO("سینما", 3));

        yield return HighlightWithMessageBottomAndWait(groupSelectionUI.transform.Find("HighlightArea") as RectTransform, TutorialArrow.Direction.Bottom, "این موضوع رو انتخاب کن تا بریم بازی رو یاد بگیریم.", true);

        groupSelectionUI.Hide();

        #endregion

        #region 4 - Start mock game

        GameAnalytics.NewDesignEvent("tutorial:introgame", 4);

        var td = TransientData.Instance;
        var totalTime = td.ConfigValues.ClientTimePerRound;
        var waitTime = TimeSpan.FromSeconds(totalTime.TotalSeconds / 4);

        roundUI.StartRound("استان‌های ایران", totalTime);
        roundUI.CanUseTimePowerup = false;
        roundUI.CanUseWordPowerup = false;
        roundUI.TimePaused = true;

        var messageHandle = HighlightWithMessageBottomAndWait(roundUI.transform.Find("AnswersContainer/Category") as RectTransform, TutorialArrow.Direction.Bottom, "این‌جا سوال این دست رو می‌بینی. تو هر دست باید عبارت‌ها و کلمه‌هایی که مربوط به سوال اون دست هستن رو وارد کنی.", false);

        // Wait for input field to initialize
        yield return null;

        roundUI.InputField.ForceDeselect();

        roundUI.transform.Find("BackButton").GetComponent<Button>().interactable = false;

        yield return messageHandle;
        GameAnalytics.NewDesignEvent("tutorial:introgame", 4.1f);

        roundUI.InputField.text = "چهارمحال و بختیاری";
        yield return HighlightWithMessageTopAndWait(roundUI.transform.Find("Answer") as RectTransform, default, "باید کلمات مورد نظرت رو دونه دونه وارد کنی. ما اولیشو برات وارد کردیم.", false);
        yield return HighlightWithMessageTopAndWait(roundUI.transform.Find("EnterButton") as RectTransform, TutorialArrow.Direction.Left, "این دکمه رو بزن تا کلمه ثبت شه و امتیاز بگیری. می‌تونی از کلید تایید کیبورد گوشیت هم استفاده کنی.", true);
        roundUI.SubmitWord();

        GameAnalytics.NewDesignEvent("tutorial:introgame", 4.2f);

        yield return HighlightWithMessageBottomAndWait(roundUI.transform.Find("AnswersContainer/AnswersList") as RectTransform, default, "آفرین، امتیاز گرفتی! حالا کلمه‌های بعدی رو وارد کن.", false);

        EventSystem.current.SetSelectedGameObject(roundUI.InputField.gameObject);
        roundUI.InputField.keepFocus = true;
        roundUI.TimePaused = false;

        #endregion

        #region 5 - Wait then show Word powerup

        GameAnalytics.NewDesignEvent("tutorial:introgame", 5);

        while (roundUI.RemainingTime >= totalTime - waitTime)
            yield return null;

        roundUI.InputField.keepFocus = false;
        roundUI.InputField.ForceDeselect();
        roundUI.CanUseWordPowerup = true;
        roundUI.TimePaused = true;

        yield return HighlightWithMessageTopAndWait(roundUI.transform.Find("HelpButton") as RectTransform, TutorialArrow.Direction.TopRight, "این دکمه رو که بزنی، یک کلمه بهت کمک می‌کنه. این دست مجانیه، بزنش تا امتیاز بیش‌تری بگیری!", true);

        if (!UseWordPowerup())
            yield return ShowMessageBoxBottomAndWait("چطور تونستی تو این مدت کم تمام جوابا رو پیدا کنی؟! آفرین!");

        EventSystem.current.SetSelectedGameObject(roundUI.InputField.gameObject);
        roundUI.InputField.keepFocus = true;
        roundUI.TimePaused = false;

        #endregion

        #region 6 - Wait again then show Time powerup

        GameAnalytics.NewDesignEvent("tutorial:introgame", 6);

        while (roundUI.RemainingTime >= totalTime - waitTime - waitTime)
            yield return null;

        roundUI.InputField.keepFocus = false;
        roundUI.InputField.ForceDeselect();
        roundUI.CanUseTimePowerup = true;
        roundUI.TimePaused = true;

        yield return HighlightWithMessageTopAndWait(roundUI.transform.Find("ExtraTimeButton") as RectTransform, TutorialArrow.Direction.TopRight, "اوه اوه، زمانت داره تموم می‌شه! این دکمه رو که بزنی، زمانتو بیش‌تر می‌کنه. این بار مهمون مایی، بزن روش!", true);

        roundUI.ExtendRemainingRoundTime(td.ConfigValues.RoundTimeExtension);

        roundUI.CanUseTimePowerup = false;
        roundUI.TimePaused = false;
        EventSystem.current.SetSelectedGameObject(roundUI.InputField.gameObject);
        roundUI.InputField.keepFocus = true;

        #endregion

        #region 7 - Wait 10 seconds and show back button

        GameAnalytics.NewDesignEvent("tutorial:introgame", 7);

        while (roundUI.RemainingTime > totalTime + td.ConfigValues.RoundTimeExtension - waitTime - waitTime - waitTime)
            yield return null;

        roundUI.InputField.keepFocus = false;
        roundUI.InputField.ForceDeselect();
        roundUI.TimePaused = true;

        var backButton = roundUI.transform.Find("BackButton") as RectTransform;
        backButton.GetComponent<Button>().interactable = true;
        yield return HighlightWithMessageBottomAndWait(backButton, TutorialArrow.Direction.BottomRight, "هر وقت فکر کردی دیگه جوابی یادت نمی‌آد، می‌تونی دست رو زودتر تموم کنی.", false);

        roundUI.TimePaused = false;
        EventSystem.current.SetSelectedGameObject(roundUI.InputField.gameObject);
        roundUI.InputField.keepFocus = true;

        #endregion

        #region 8 - Wait for end of game

        GameAnalytics.NewDesignEvent("tutorial:introgame", 8);

        while (roundUI.RemainingTime > TimeSpan.Zero)
            yield return null;

        roundUI.InputField.keepFocus = false;
        roundUI.InputField.ForceDeselect();
        SoundEffectManager.Play(SoundEffect.RoundTimeFinished);
        roundUI.Hide();

        #endregion

        #region 9 - Show results screen, vote buttons and all answers button

        GameAnalytics.NewDesignEvent("tutorial:introgame", 9);

        var roundInfo = new RoundDisplayInfo
        {
            category = "استان‌های ایران",
            haveAnswers = false,
            myScore = (uint)playedWords.Sum(x => x.score),
            index = 0,
            myWords = playedWords.Select(x => new FLGameLogic.WordScorePair(x.word, (byte)x.score)).ToList(),
            state = RoundState.WaitingForThem,
            roundRated = false,
            theirWords = Array.Empty<FLGameLogic.WordScorePair>()
        };

        overviewUI.ShowGame("مداد جنگی", 0, 1, 0, new[] { roundInfo }, false, false, false);

        yield return HighlightWithMessageBottomAndWait(overviewUI.transform.Find("ActionBar/BackButton") as RectTransform, TutorialArrow.Direction.BottomRight, $"هر بازی {TransientData.Instance.ConfigValues.NumRoundsPerGame} دست ادامه پیدا می‌کنه. بعد از بازی کردن نوبت خودت باید منتظر حریف بمونی تا نوبت خودشو بازی کنه.", true);

        overviewUI.Hide();

        #endregion

        GameAnalytics.NewDesignEvent("tutorial:introgame", 10);

        transform.Find("Game").gameObject.SetActive(false);
        yield return MenuManager.Instance.Show<MainMenu>().RunAsCoroutine();

        HideAll();
        SetStepComplete(TutorialStep.IntroductoryGame);

        yield return HighlightWithMessageBottomAndWait(startGameButton.transform as RectTransform, TutorialArrow.Direction.Top, "حالا این دکمه رو لمس کن تا اولین بازی واقعیت رو شروع کنی! موفق باشی!", true);
        MenuManager.Instance.Menu<MainMenu>().StartNewGame();

        FinishTutorialSequence(token);
    }

    Transform GetFirstActiveChild(Transform parent)
    {
        for (var i = 0; i < parent.childCount; ++i)
            if (parent.GetChild(i).gameObject.activeInHierarchy)
            {
                return parent.GetChild(i);
            }

        return null;
    }

    HashSet<string> answers = new HashSet<string>
    {
        "سیستان و بلوچستان",
        "کهگیلویه و بویر احمد",
        "سمنان",
        "یزد",
        "خراسان شمالی",
        "خراسان رضوی",
        "خراسان جنوبی",
        "چهارمحال و بختیاری",
        "بوشهر",
        "البرز",
        "فارس",
        "قزوین",
        "خوزستان",
        "کرمان",
        "کرمانشاه",
        "ایلام",
        "لرستان",
        "مرکزی",
        "قم",
        "همدان",
        "کردستان",
        "زنجان",
        "آذربایجان غربی",
        "آذربایجان شرقی",
        "اردبیل",
        "هرمزگان",
        "گیلان",
        "مازندران",
        "گلستان",
        "اصفهان",
        "تهران",
    };

    List<(string word, int score)> playedWords = new List<(string word, int score)>();

    public void PlayWord(string word)
    {
        word = word.Trim().Replace('ي', 'ی').Replace('ك', 'ک');
        if (!playedWords.Any(x => x.word == word) && answers.Contains(word))
        {
            var score = Random.Range(2, 4);
            roundUI.AddWord(word, score, false);
            playedWords.Add((word, score));
        }
        else if (playedWords.Any(x => x.word == word))
            roundUI.AddWord(word, 0, true);
        else
            roundUI.AddWord(word, 0, false);
    }

    public bool UseWordPowerup()
    {
        var word = answers.FirstOrDefault(w => !playedWords.Any(x => x.word == w));
        if (word == null)
            return false;
        PlayWord(word);
        return true;
    }
}
