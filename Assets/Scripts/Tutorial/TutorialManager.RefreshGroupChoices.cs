using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    public void GroupChoicesShown()
    {
        if (!GetStepComplete(TutorialStep.GroupChange) && TransientData.Instance.GetStatisticValue(Network.Types.Statistics.RoundsCompleted) >= 4)
            StartCoroutine(RunChangeGroupChoices());
    }

    IEnumerator RunChangeGroupChoices()
    {
        var token = StartTutorialSequence();

        yield return HighlightWithMessageTopAndWait(GroupSelectionUI.Instance.RefreshGroupsButton, default, "اگه موضوع‌هایی که نشون داده شده رو دوست نداری، می‌تونی با لمس این دکمه عوضشون کنی.", false);

        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.GroupChange);
    }
}
