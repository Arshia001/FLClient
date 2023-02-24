using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    IEnumerator RunShowAccountSettings()
    {
        yield return null; // This could be called at startup, before TutorialManager.Start has a chance to clear everything out

        var token = StartTutorialSequence();

        yield return ShowMessageBoxBottomAndWait("برای این که اطلاعاتت رو از دست ندی، می‌تونی برای خودت حساب درست کنی.");

        yield return HighlightWithMessageBottomAndWait(Footer.Instance.SettingsIconTransform, TutorialArrow.Direction.TopRight, "این‌جا رو لمس کن.", true);
        yield return MenuManager.Instance.Show<SettingsMenu>().RunAsCoroutine();

        yield return HighlightWithMessageBottomAndWait(SettingsMenu.Instance.AccountSettingsButtonTransform, TutorialArrow.Direction.Top, "با لمس این دکمه می‌تونی حسابتو بسازی. نام کاربریت رو هم می‌تونی از همین‌جا تغییر بدی.", true);
        SettingsMenu.Instance.ShowAccount();

        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.CreateAccount);
    }
}
