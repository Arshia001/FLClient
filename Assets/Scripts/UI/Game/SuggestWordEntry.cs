using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SuggestWordEntry : MonoBehaviour
{
    [SerializeField] Sprite checkboxChecked = default;
    [SerializeField] Sprite checkboxUnchecked = default;

    TextMeshProUGUI text;
    Image checkbox;

    public bool Selected { get; private set; }
    public string Word { get; private set; }

    void Awake()
    {
        text = transform.Find("Word").GetComponent<TextMeshProUGUI>();
        checkbox = transform.Find("Checkbox").GetComponent<Image>();
    }

    public void Initialize(string word)
    {
        Selected = false;
        Word = word;
        Translation.SetTextNoTranslate(text, word);
        RefreshCheckbox();
    }

    void RefreshCheckbox() =>
        checkbox.sprite = Selected ? checkboxChecked : checkboxUnchecked;

    public void Toggle()
    {
        Selected = !Selected;
        RefreshCheckbox();
    }
}
