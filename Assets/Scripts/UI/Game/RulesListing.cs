using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RulesListing : MonoBehaviour
{
    const string template = @"
هر بازی {0} نوبت طول می‌کشه. نفر اول یک نوبت بازی می‌کنه، بعد نفر دوم دو نوبت بازی می‌کنه، بعد هر نفر دو نوبت بازی می‌کنه تا بازی تموم شه.
در هر نوبت کسی که مجموع ارزش کلماتش بیش‌تره یک امتیاز به دست می‌آره. اگه دو نفر مساوی بشن هر دو امتیاز می‌گیرن.
کسی که نوبت رو شروع می‌کنه می‌تونه موضوع اون نوبت رو انتخاب کنه.
هر نوبت {1}‌ زمان داره و این زمان یک بار به اندازه {2} قابل افزایشه.
هر کلمه یا عبارتی که وارد می‌شه اگر حداکثر یک چهارم حروفش اشتباه باشن، به طور خودکار تصحیح می‌شه و امتیاز بهش تعلق می‌گیره.
هر نفر بعد از شروع نوبتش {3} وقت داره تا بازی کنه. اگه این زمان تموم بشه، بازی به شکل خودکار به نفر روبرو واگذار می‌شه. این قانون برای نفر اول در اولین نوبت اعمال نمی‌شه.
بعد از اتمام بازی، به هر نفر تجربه، امتیاز و سکه تعلق می‌گیره.
برنده {4} و بازنده {5} سکه دریافت می‌کنن. در صورت مساوی شدن، هر نفر {6} سکه می‌گیره.
همچنین، برنده {7} و بازنده {8} تجربه می‌گیرن. در صورت مساوی شدن، هر نفر {9} تجربه می‌گیره.
امتیاز بر اساس اختلاف امتیاز فعلی پخش می‌شه. هر چقدر امتیاز فعلی برنده از بازنده کم‌تر باشه، امتیاز بیشتری به دست می‌آره. برنده در هر بازی حداقل {10} و حداکثر {11} امتیاز به دست می‌آره. از بازنده به اندازه {12} درصد امتیازی که برنده به دست آورده، کم می‌شه.
";

    MenuBackStackHandler backStackHandler;

    void Awake()
    {
        backStackHandler = new MenuBackStackHandler(() =>
        {
            Hide();
            return true;
        });
        Hide();
    }

    void Start()
    {
        var conf = TransientData.Instance.ConfigValues;
        var text = string.Format(template.Trim(),
            conf.NumRoundsPerGame,
            conf.ClientTimePerRound.FormatAsPersianExpression(),
            conf.RoundTimeExtension.FormatAsPersianExpression(),
            conf.GameInactivityTimeout.FormatAsPersianExpression(),
            conf.WinnerGoldGain,
            conf.LoserGoldGain,
            conf.DrawGoldGain,
            conf.WinnerXPGain,
            conf.LoserXPGain,
            conf.DrawXPGain,
            conf.MinScoreGain,
            conf.MaxScoreGain,
            (int)(conf.LoserScoreLossRatio * 100)
            );

        Translation.SetTextNoTranslate(transform.Find("Frame/Scroll View/Viewport/Content/Text").GetComponent<TextMeshProUGUI>(), text);
    }

    public void Show()
    {
        backStackHandler.MenuShown();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);
    }
}
