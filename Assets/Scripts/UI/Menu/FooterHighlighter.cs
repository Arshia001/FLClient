using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FooterHighlighter : MonoBehaviour
{
    RectTransform target;
    RectTransform rectTransform;

    public FooterIcon Target
    {
        set => target = value.transform as RectTransform;
    }

    void Awake() => rectTransform = transform as RectTransform;

    void Update()
    {
        rectTransform.localPosition = Vector3.Lerp(rectTransform.localPosition, target.localPosition, Time.deltaTime * 6);
    }

    public void SetTargetWithoutAnimation(FooterIcon icon)
    {
        Target = icon;

        StartCoroutine(DoSetTargetWithoutAnimation());
    }

    IEnumerator DoSetTargetWithoutAnimation()
    {
        yield return null;

        rectTransform.localPosition = target.localPosition;
    }
}
