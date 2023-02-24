using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Footer : SingletonBehaviour<Footer>
{
    public enum FooterIconType
    {
        Settings,
        LeaderBoard,
        Home,
        Profile,
        Shop
    }

    FooterHighlighter highlighter;

    Dictionary<FooterIconType, FooterIcon> icons = new Dictionary<FooterIconType, FooterIcon>();

    public RectTransform SettingsIconTransform => icons[FooterIconType.Settings].transform as RectTransform;
    public RectTransform ShopIconTransform => icons[FooterIconType.Shop].transform as RectTransform;

    protected override void Awake()
    {
        base.Awake();

        highlighter = transform.Find("Highlighter").GetComponent<FooterHighlighter>();

        foreach (FooterIconType val in Enum.GetValues(typeof(FooterIconType)))
            icons[val] = transform.Find(val.ToString()).GetComponent<FooterIcon>();
    }

    public void SetActivePage(FooterIconType icon)
    {
        highlighter.Target = icons[icon];
        foreach (var kv in icons)
            kv.Value.Selected = kv.Key == icon;
    }

    public void SetActivePageWithoutAnimation(FooterIconType icon)
    {
        highlighter.SetTargetWithoutAnimation(icons[icon]);
        foreach (var kv in icons)
            kv.Value.SetSelectedWithoutAnimation(kv.Key == icon);
    }
}
