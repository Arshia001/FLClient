using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FooterIcon : MonoBehaviour
{
    Image icon;
    LayoutElement layoutElement;

    public bool Selected { get; set; }

    void Awake()
    {
        icon = transform.Find("Icon").GetComponent<Image>();
        layoutElement = GetComponent<LayoutElement>();
    }

    void Update()
    {
        var color = Color.white;
        color.a = Mathf.Lerp(icon.color.a, Selected ? 1.0f : 0.6f, Time.deltaTime * 6);
        icon.color = color;

        layoutElement.flexibleWidth = Mathf.Lerp(layoutElement.flexibleWidth, Selected ? 1.8f : 1.0f, Time.deltaTime * 6);

        icon.transform.localScale = Mathf.Lerp(icon.transform.localScale.x, Selected ? 1.2f : 1.0f, Time.deltaTime * 6) * Vector3.one;
    }

    public void SetSelectedWithoutAnimation(bool selected)
    {
        Selected = selected;
        icon.color = selected ? Color.white : new Color(1, 1, 1, 0.6f);
        layoutElement.flexibleHeight = selected ? 1.8f : 1.0f;
        icon.transform.localScale = (selected ? 1.2f : 1.0f) * Vector3.one;
    }
}
