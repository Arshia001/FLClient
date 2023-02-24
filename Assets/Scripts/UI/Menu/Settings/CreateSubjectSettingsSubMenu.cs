using System.Collections;
using System.Collections.Generic;
using Network;
using TMPro;
using UnityEngine;

public class CreateSubjectSettingsSubMenu : SettingsSubMenu
{
    ImmInputField nameText, wordsText;

    protected override void Awake()
    {
        base.Awake();

        nameText = transform.Find("SubjectName").GetComponent<ImmInputField>();
        wordsText = transform.Find("Words").GetComponent<ImmInputField>();
    }

    public override void Show()
    {
        base.Show();

        nameText.text = "";
        wordsText.text = "";
    }

    public void OK() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var name = nameText.text;
        var words = wordsText.text;

        if (string.IsNullOrEmpty(name))
        {
            InformationToast.Instance.Enqueue("هنوز سوال رو وارد نکردی.");
            return;
        }

        if (string.IsNullOrEmpty(words))
        {
            InformationToast.Instance.Enqueue("هنوز هیچ کلمه‌ای وارد نکردی.");
            return;
        }

        using (LoadingIndicator.Show(true))
            await ConnectionManager.Instance.EndPoint<SuggestionEndPoint>().SuggestCategory(name, words);

        SettingsMenu.Instance.HideSubMenus();

        DialogBox.Instance.Show("موضوعت ثبت شد و بررسی می‌شه. بعد از تایید موضوعت، جایزشو می‌گیری. بررسی موضوعات ممکنه چند روز طول بکشه. ممنون که وقت گذاشتی!", "باشه").Ignore();
    });
}
