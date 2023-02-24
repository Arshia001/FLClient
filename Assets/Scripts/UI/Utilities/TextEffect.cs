using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TextEffect : MonoBehaviour
{
    [SerializeField] Material effect1 = default;
    [SerializeField] Material effect2 = default;

    TextMeshProUGUI text;
    TextMeshProUGUI effectText1, effectText2;

    void Awake() => text = GetComponent<TextMeshProUGUI>();

    void Start() => RefreshText();

    void OnEnable()
    {
        effectText1?.gameObject.SetActive(true);
        effectText2?.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        effectText1?.gameObject.SetActive(false);
        effectText2?.gameObject.SetActive(false);
    }

    TextMeshProUGUI CreateEffectText(Material effect, int siblingIndex)
    {
        var go = Instantiate(gameObject);
        foreach (var component in go.GetComponents<Component>())
            if (component.GetType() != typeof(TextMeshProUGUI) &&
                component.GetType() != typeof(RectTransform) &&
                component.GetType() != typeof(CanvasRenderer))
                Destroy(component);
        go.name = gameObject.name + "_" + effect.name;
        go.SetActive(gameObject.activeSelf);
        var tr = go.transform;
        tr.SetParent(transform.parent, false);
        tr.SetSiblingIndex(siblingIndex);
        var lg = go.AddComponent<LayoutElement>();
        lg.ignoreLayout = true;
        var effectText = tr.GetComponent<TextMeshProUGUI>();
        effectText.color = Color.white;
        effectText.fontMaterial = effect;
        return effectText;
    }

    public void RefreshText()
    {
        if (!text)
            return;

        var content = text.text;
        var rtl = text.isRightToLeftText;
        var siblingIndex = transform.GetSiblingIndex();

        if (effect2 != null)
        {
            if (effectText2 == null)
                effectText2 = CreateEffectText(effect2, siblingIndex);

            effectText2.text = content;
            effectText2.isRightToLeftText = rtl;
        }

        if (effect1 != null)
        {
            if (effectText1 == null)
                effectText1 = CreateEffectText(effect1, siblingIndex);

            effectText1.text = content;
            effectText1.isRightToLeftText = rtl;
        }
    }

    void UpdateRect(RectTransform effect)
    {
        var rect = transform as RectTransform;
        effect.anchorMin = rect.anchorMin;
        effect.anchorMax = rect.anchorMax;
        effect.anchoredPosition = rect.anchoredPosition;
        effect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.rect.width);
        effect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.height);
    }

    void LateUpdate()
    {
        if (effectText1 != null)
            UpdateRect(effectText1.rectTransform);
        if (effectText2 != null)
            UpdateRect(effectText2.rectTransform);
    }
}
