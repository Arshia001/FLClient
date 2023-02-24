using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TutorialManager
{
    IEnumerator RunShowEditAvatar()
    {
        yield return null; // This could be called at startup, before TutorialManager.Start has a chance to clear everything out

        var token = StartTutorialSequence();

        yield return HighlightWithMessageBottomAndWait(Footer.Instance.ShopIconTransform, TutorialArrow.Direction.TopLeft, "بیا نشونت بدم چطور می‌تونی آواتارتو بسازی. این‌جا رو لمس کن.", true);
        yield return MenuManager.Instance.Show<ShopMenu>().RunAsCoroutine();

        var shopMenu = MenuManager.Instance.Menu<ShopMenu>();
        yield return HighlightWithMessageBottomAndWait(shopMenu.AvatarShopButtonTransform, TutorialArrow.Direction.Bottom, "برای ورود به بخش ساخت آواتار، این‌جا رو لمس کن.", true);
        shopMenu.ShowAvatarShop();

        yield return HighlightWithMessageBottomAndWait(shopMenu.AvatarShop.AcceptButtonTransform, TutorialArrow.Direction.Left, "با ساختن یه آواتار خفن، به حریفات نشون بده با کی طرفن! یادت نره بعد از ساختن آواتار دکمه‌ی تایید رو بزنی.", false);

        FinishTutorialSequence(token);

        SetStepComplete(TutorialStep.EditAvatar);
    }
}
