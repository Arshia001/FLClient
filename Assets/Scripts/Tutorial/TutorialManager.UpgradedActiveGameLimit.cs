using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    public bool NeedToActivateUpgradedActiveGameLimitMode()
    {
        if (!GetStepComplete(TutorialStep.UpgradedActiveGameLimitMode))
        {
            StartCoroutine(RunUpgradedActiveGameLimitMode());
            return true;
        }

        return false;
    }

    IEnumerator RunUpgradedActiveGameLimitMode()
    {
        var token = StartTutorialSequence();

        var td = TransientData.Instance;
        yield return HighlightWithMessageTopAndWait(UnlimitedPlayButton.Instance.transform as RectTransform, TutorialArrow.Direction.Bottom, 
            $"در هر لحظه بیش‌تر از {td.ConfigValues.MaxActiveGames} بازی فعال نمی‌تونی داشته باشی، " +
            $"مگر این که افزایش تعداد بازی رو فعال کرده باشی. هر بار که فعالش کنی، " +
            $"تا مدتی می‌تونی {td.ConfigValues.MaxActiveGamesWhenUpgraded} بازی فعال داشته باشی.", false);

        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.UpgradedActiveGameLimitMode);
    }
}
