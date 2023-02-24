using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PersianTextLineFixer : MonoBehaviour
{
    public bool forceRtl, forceLtr;

    Text text;


    void Awake()
    {
        text = GetComponent<Text>();

        SetText(text, text.text, forceRtl, forceLtr);
    }

    static bool IsTextRTL(string text, bool forceRtl, bool forceLtr)
    {
        if (forceRtl)
            return true;
        else if (forceLtr)
            return false;

        for (int i = 0; i < text.Length; ++i)
            switch (PersianTextShaper.PersianTextShaper.GetCharType(text[i]))
            {
                case PersianTextShaper.PersianTextShaper.CharType.LTR:
                    return false;
                case PersianTextShaper.PersianTextShaper.CharType.RTL:
                    return true;
            }

        return true;
    }

    public static void SetText(Text text, string unshapedText, bool forceRtl = false, bool forceLtr = false)
    {
        if (string.IsNullOrEmpty(unshapedText))
        {
            text.text = "";
            return;
        }

        unshapedText = unshapedText.Replace("\r\n", "\n");

        bool rtl = IsTextRTL(unshapedText, forceRtl, forceLtr);

        var tempShapedText = PersianTextShaper.PersianTextShaper.ShapeText(unshapedText, rtl, true);

        TextGenerator generator = new TextGenerator();
        var extents = (text.transform as RectTransform).rect.size;
        var layoutElem = text.GetComponent<LayoutElement>();
        if (layoutElem && layoutElem.minWidth > extents.x)
            extents.x = layoutElem.minWidth;
        if (text.horizontalOverflow == HorizontalWrapMode.Wrap)
            extents.y = 100000000; // We don't care about height (gonna be clipped anyway), only width, so set the height really high; helps with auto-resize scenarios
        var settings = text.GetGenerationSettings(extents);

        generator.Populate(tempShapedText, settings);

        var lines = generator.lines;
        List<string> lineTexts = new List<string>();
        for (int i = 0; i < lines.Count - 1; ++i)
            lineTexts.Add(unshapedText.Substring(lines[i].startCharIdx, Mathf.Min(unshapedText.Length, lines[i + 1].startCharIdx) - lines[i].startCharIdx));
        if (lines[lines.Count - 1].startCharIdx < unshapedText.Length)
            lineTexts.Add(unshapedText.Substring(lines[lines.Count - 1].startCharIdx));

        var sb = new StringBuilder();
        for (int i = 0; i < lineTexts.Count; ++i)
            sb.AppendLine(PersianTextShaper.PersianTextShaper.ShapeText(lineTexts[i].Trim(), rtl));

        text.text = sb.ToString().Trim('\n');

        if (layoutElem != null)
        {
            int numLines = generator.lineCount;
            layoutElem.minHeight = (numLines + 1) * text.fontSize + numLines * (text.lineSpacing + 1);
        }
    }
}
