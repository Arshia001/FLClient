using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Translation : MonoBehaviour
{
    public string TextKey;


    public static void SetText(Text uiText, string textKey)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(textKey))
        {
#if en_US
            uiText.text = lm.Translate(textKey);
#else
            PersianTextLineFixer.SetText(uiText, lm.Translate(textKey), true);
#endif
        }

        uiText.font = lm.GetReplacementFont(uiText.font);
    }

    public static void SetTextNoTranslate(Text uiText, string text)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(text))
        {
#if en_US
            uiText.text = text;
#else
            PersianTextLineFixer.SetText(uiText, text, true);
#endif
        }
        else
            uiText.text = "";

        uiText.font = lm.GetReplacementFont(uiText.font);
    }

    public static void SetText(TextMeshProUGUI textMesh, string textKey)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(textKey))
        {
#if en_US
            textMesh.text = lm.Translate(textKey);
            textMesh.isRightToLeftText = false;
#else
            textMesh.text = PersianTextShaper.PersianTextShaper.ShapeText(lm.Translate(textKey), rightToLeftRenderDirection: true);
            textMesh.isRightToLeftText = true;
#endif
        }
        else
            textMesh.text = "";

        textMesh.font = lm.GetReplacementFont(textMesh.font);
        var textEffect = textMesh.gameObject.GetComponent<TextEffect>();
        if (textEffect)
            textEffect.RefreshText();
    }

    public static void SetTextNoTranslate(TextMeshProUGUI textMesh, string text)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(text))
        {
#if en_US
            textMesh.text = text;
            textMesh.isRightToLeftText = false;
#else
            textMesh.text = PersianTextShaper.PersianTextShaper.ShapeText(text, rightToLeftRenderDirection: true);
            textMesh.isRightToLeftText = true;
#endif
        }
        else
            textMesh.text = "";

        textMesh.font = lm.GetReplacementFont(textMesh.font);
        var textEffect = textMesh.gameObject.GetComponent<TextEffect>();
        if (textEffect)
            textEffect.RefreshText();
    }

    public static void SetTextNoShape(TextMeshProUGUI textMesh, string text)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(text))
            textMesh.text = text;
        else
            textMesh.text = "";

        textMesh.font = lm.GetReplacementFont(textMesh.font);
        var textEffect = textMesh.gameObject.GetComponent<TextEffect>();
        if (textEffect)
            textEffect.RefreshText();
    }

    public static void SetTextNoTranslate(TMP_InputField textMesh, string text)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(text))
        {
#if en_US
            textMesh.text = text;
            textMesh.isRightToLeftText = false;
#else
            textMesh.text = PersianTextShaper.PersianTextShaper.ShapeText(text, rightToLeftRenderDirection: true);
            textMesh.textComponent.isRightToLeftText = true;
#endif
        }
        else
            textMesh.text = "";

        textMesh.textComponent.font = lm.GetReplacementFont(textMesh.textComponent.font);
    }

    public static void SetText(TextMesh textMesh, string textKey)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(textKey))
        {
#if en_US
            textMesh.text = lm.Translate(textKey);
#else
            textMesh.text = PersianTextShaper.PersianTextShaper.ShapeText(lm.Translate(textKey));
#endif
        }

        textMesh.font = lm.GetReplacementFont(textMesh.font);
        textMesh.GetComponent<Renderer>().material = textMesh.font.material;
    }

    public static void SetTextNoTranslate(TextMesh textMesh, string text)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(text))
        {
#if en_US
            textMesh.text = text;
#else
            textMesh.text = PersianTextShaper.PersianTextShaper.ShapeText(text);
#endif
        }
        else
            textMesh.text = "";

        textMesh.font = lm.GetReplacementFont(textMesh.font);
        textMesh.GetComponent<Renderer>().material = textMesh.font.material;
    }

    public static void SetTextNoTranslate(TextMeshPro textMesh, string text)
    {
        var lm = LocalizationManager.Instance;

        if (!string.IsNullOrEmpty(text))
        {
#if en_US
            textMesh.text = text;
#else
            textMesh.text = PersianTextShaper.PersianTextShaper.ShapeText(text);
#endif
        }
        else
            textMesh.text = "";

        textMesh.font = lm.GetReplacementFont(textMesh.font);
        textMesh.GetComponent<Renderer>().material = textMesh.font.material;
    }

    void Start()
    {
        var text = GetComponent<Text>();
        if (text != null)
            SetText(text, TextKey);

        var textMesh = GetComponent<TextMesh>();
        if (textMesh != null)
            SetText(textMesh, TextKey);
    }
}
