using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PersianTextShaper.PersianTextShaper;

[RequireComponent(typeof(ImmInputField))]
public class PersianInputField : MonoBehaviour
{
    ImmInputField inputField;

    void Awake()
    {
        inputField = GetComponent<ImmInputField>();
        inputField.textComponent.isRightToLeftText = true;
        inputField.onModifyDisplayedText += InputField_onModifyDisplayedText;
    }

    void SetHorizontalAlignment(int alignment)
    {
        var a = (int)inputField.textComponent.alignment;
        a &= 0x7fffff00;
        a |= 0x1 << alignment;
        inputField.textComponent.alignment = (TMPro.TextAlignmentOptions)a;
    }

    private string InputField_onModifyDisplayedText(string original)
    {
        var textType = CharType.Space;
        foreach (var ch in original)
        {
            var type = GetCharType(ch);
            if (type == CharType.LTR || type == CharType.RTL)
            {
                textType = type;
                break;
            }
        }

        bool rtl;
        switch (textType)
        {
            case CharType.Space:
            case CharType.RTL:
                SetHorizontalAlignment(2); // right
                inputField.textComponent.isRightToLeftText = true;
                rtl = true;
                break;

            default:
                SetHorizontalAlignment(0); // left
                inputField.textComponent.isRightToLeftText = false;
                rtl = false;
                break;
        }

        return ShapeText(original, rightToLeft: rtl, rightToLeftRenderDirection: rtl, preserveCharacterCount: true);
    }
}
