using GameAnalyticsSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    public void BackToOverviewOnOpponentsTurn()
    {
#if UNITY_EDITOR
        if (debug_skipIntroGameInEditor)
            return;
#endif

        if (!GetStepComplete(TutorialStep.RoundInfo))
            StartCoroutine(RunShowRoundInfo());
    }

    IEnumerator RunShowRoundInfo()
    {
        var token = StartTutorialSequence();

        var overviewUI = GameManager.Instance.Menu<OverviewUI>();

        yield return ShowMessageBoxBottomAndWait("بعد از دست اول، باید منتظر حریف بمونی. بعدش هر نفر دو دست بازی می‌کنه و نوبتو واگذار می‌کنه.");

        yield return HighlightWithMessageTopAndWait(overviewUI.transform.Find("RoundDetails/AnswersList") as RectTransform, default, "کلماتی که خودت و حریفت بازی کردین با امتیازشون این‌جا نمایش داده می‌شه.", false);

        yield return HighlightWithMessageTopAndWait(overviewUI.transform.Find("RoundDetails/ShowAllAnswersButton") as RectTransform, TutorialArrow.Direction.Bottom, "برای دیدن تمام جواب‌ها، می‌تونی این دکمه رو لمس کنی.", false);

        yield return HighlightWithMessageTopAndWait(overviewUI.transform.Find("RoundDetails/VotePending") as RectTransform, TutorialArrow.Direction.Top, "بعد از هر دست، می‌تونی به سوال اون دست امتیاز بدی. این به ما کمک می‌کنه سوالای جذاب‌تری برات آماده کنیم.", true);
        overviewUI.RoundDetails.Like();
        
        overviewUI.RoundDetails.ForceActivateSuggestWordsButton();
        yield return HighlightWithMessageTopAndWait(overviewUI.transform.Find("RoundDetails/VoteDone/SuggestWords") as RectTransform, default, "اگه جوابی رو نوشتی که فکر می‌کنی درسته ولی امتیاز نگرفتی، می‌تونی با لمس این دکمه پیشنهاد کنی که جوابت به بازی اضافه بشه. برای هر پیشنهاد درست، سکه هدیه می‌گیری!", false);
        yield return HighlightAndWait(overviewUI.transform.Find("RoundDetails/VoteDone/ContinueButton") as RectTransform, TutorialArrow.Direction.Top);

        overviewUI.RoundDetails.Hide();

        yield return HighlightWithMessageBottomAndWait(overviewUI.transform.Find("Rounds") as RectTransform, default, "هر دستی که بازی می‌کنی، نتیجه‌ش این‌جا اضافه می‌شه. می‌تونی رو هر کدوم که خواستی بزنی تا جزئیات رو ببینی.", false);

        yield return HighlightWithMessageTopAndWait(overviewUI.transform.Find("ActionBar/InfoButton") as RectTransform, TutorialArrow.Direction.BottomLeft, "می‌تونی قوانین کامل بازی رو از این‌جا بخونی.", false);

        yield return HighlightWithMessageBottomAndWait(overviewUI.BackButtonTransform, TutorialArrow.Direction.BottomRight, "تا حریفت بازی کنه می‌تونی برگردی و از منوی اصلی یه بازی دیگه رو شروع کنی!", false);
        
        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.RoundInfo);
    }
}
