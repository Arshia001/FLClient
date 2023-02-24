using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    IEnumerator RunEditBazaarAccountUserName()
    {
        yield return null; // This could be called at startup, before TutorialManager.Start has a chance to clear everything out

        var token = StartTutorialSequence();

        yield return ShowMessageBoxBottomAndWait("بیا نشونت بدم نام کاربریتو از کجا عوض کنی!");

        yield return HighlightWithMessageBottomAndWait(Footer.Instance.SettingsIconTransform, TutorialArrow.Direction.TopRight, "این‌جا رو لمس کن.", true);
        yield return MenuManager.Instance.Show<SettingsMenu>().RunAsCoroutine();

        yield return HighlightWithMessageBottomAndWait(SettingsMenu.Instance.AccountSettingsButtonTransform, TutorialArrow.Direction.Top, "هر وقت بخوای می‌تونی نام کاربریت رو از این‌جا تغییر بدی.", true);
        SettingsMenu.Instance.ShowAccount();

        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.CreateAccount);
    }
}
