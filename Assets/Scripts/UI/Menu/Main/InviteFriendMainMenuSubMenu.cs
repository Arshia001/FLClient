using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InviteFriendMainMenuSubMenu : MainMenuSubMenu
{
    void Start()
    {
        var td = TransientData.Instance;

        Translation.SetTextNoTranslate(transform.Find("Description").GetComponent<TextMeshProUGUI>(), $"می‌تونی با دعوت کردن هر دوستت {td.ConfigValues.InviterReward} سکه به دست بیاری! کافیه این کد رو به هر کدوم از دوستات بدی تا موقع ساختن حسابش وارد کنه و هر دو نفرتون جایزه بگیرین:");
        Translation.SetTextNoShape(transform.Find("InviteCode").GetComponent<TextMeshProUGUI>(), td.InviteCode);
    }

    public void ShareTapped()
    {
        var td = TransientData.Instance;
        AndroidShare.Share("مداد جنگی رو نصب کن!",
            $"من مداد جنگی بازی می‌کنم و اسمم {td.UserName.Value} هست. با کد دعوت {TransientData.Instance.InviteCode} بیا تو بازی تا کلی سکه جایزه بگیری و با هم بازی کنیم!\n" +
            $"اگر بازی رو نداری از این‌جا دانلودش کن:\n" +
            $"https://cafebazaar.ir/app/ir.onehand.pwclient");
    }
}
