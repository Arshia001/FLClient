using FLGameLogic;
using Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class SuggestWordsUI : MonoBehaviour
{
    List<SuggestWordEntry> entries = new List<SuggestWordEntry>();
    string category;

    Transform entryListContainer;
    GameObject entryTemplate;

    MenuBackStackHandler backStackHandler;

    void Awake()
    {
        entryListContainer = transform.Find("Entries/Viewport/Content");
        entryTemplate = entryListContainer.Find("Template").gameObject;

        backStackHandler = new MenuBackStackHandler(() =>
        {
            Hide();
            return true;
        });
    }

    public void Show(string category, IEnumerable<string> words)
    {
        this.category = category;

        backStackHandler.MenuShown();

        entries.Clear();
        entryListContainer.ClearContainer();
        foreach (var word in words)
        {
            var tr = entryListContainer.AddListItem(entryTemplate);
            var entry = tr.GetComponent<SuggestWordEntry>();
            entry.Initialize(word);
            entries.Add(entry);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);
    }

    public void OK()
    {
        var selectedWords = entries.Where(e => e.Selected).Select(e => e.Word).ToList();

        if (!selectedWords.Any())
        {
            InformationToast.Instance.Enqueue("هنوز هیچ جوابی رو انتخاب نکردی. باید حداقل یکی از جواب‌ها رو انتخاب کنی.");
            return;
        }

        TaskExtensions.RunIgnoreAsync(async () =>
        {
            using (LoadingIndicator.Show(true))
                await ConnectionManager.Instance.EndPoint<SuggestionEndPoint>().SuggestWord(category, selectedWords);

            await DialogBox.Instance.Show("ما جواب‌ها رو بررسی می‌کنیم و اگه درست باشن سکه‌هات رو می‌گیری. بررسی جواب‌ها ممکنه چند روز زمان ببره. ممنون که وقت گذاشتی!", "خب");
            Hide();
        });
    }
}
