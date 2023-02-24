using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialHighlight : MonoBehaviour
{
    const float padding = 10.0f;

    RectTransform rectTransform;

    RectTransform target;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        Hide();
    }

    void Update() => UpdateTransform();

    private void UpdateTransform()
    {
        var rect = target.GetWorldSpaceRect();

        var center = transform.parent.InverseTransformPoint(rect.center);
        transform.localPosition = center;

        var width = rect.width / transform.lossyScale.x + padding;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);

        var height = rect.height / transform.lossyScale.y + padding;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public void Show(RectTransform target)
    {
        this.target = target;
        gameObject.SetActive(true);
        Update();
    }

    public void Hide()
    {
        target = null;
        gameObject.SetActive(false);
    }

    public void Tapped() => TutorialManager.Instance.HighlightedAreaTapped();
}
