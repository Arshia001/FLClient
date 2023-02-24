using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    IEnumerator RunShowCreateSubject()
    {
        yield return null; // This could be called at startup, before TutorialManager.Start has a chance to clear everything out

        var token = StartTutorialSequence();

        yield return ShowMessageBoxBottomAndWait("راستی، می‌دونستی می‌تونی سوال‌های خودتو هم طراحی کنی؟");

        yield return HighlightWithMessageBottomAndWait(Footer.Instance.SettingsIconTransform, TutorialArrow.Direction.TopRight, "این‌جا رو لمس کن.", true);
        yield return MenuManager.Instance.Show<SettingsMenu>().RunAsCoroutine();

        yield return HighlightWithMessageBottomAndWait(SettingsMenu.Instance.CreateSubjectButtonTransform, TutorialArrow.Direction.Top, "با لمس این دکمه می‌تونی سوال‌های خودتو طراحی کنی. اگه سوالی که طراحی  کردی تایید بشه، کلی سکه جایزه می‌گیری!", true);
        SettingsMenu.Instance.ShowCreateSubject();

        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.CreateSubject);
    }
}
