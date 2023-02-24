using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GoldDisplay : MonoBehaviour
{
    public enum SpritePosition
    {
        None,
        Before,
        After
    }

    [SerializeField] SpritePosition spritePosition = SpritePosition.None;

    TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();

        var td = TransientData.Instance;
        td.Gold.ValueChanged += Gold_ValueChanged;
        UpdateDisplay(td.Gold);
    }

    private void Gold_ValueChanged(ulong newValue) => UpdateDisplay(newValue);

    void UpdateDisplay(ulong amount) => Translation.SetTextNoShape(text, GetText(amount, spritePosition));

    public static string GetText(ulong amount, SpritePosition spritePosition) =>
        AppendSprite(PersianTextShaper.PersianTextShaper.ShapeText(amount.ToString()), spritePosition);

    static string AppendSprite(string text, SpritePosition spritePosition) =>
        spritePosition == SpritePosition.None ? text :
        spritePosition == SpritePosition.Before ? $"{MoneySprites.SingleCoin} {text}" : $"{text} {MoneySprites.SingleCoin}";

    void OnDestroy() => TransientData.Instance.Gold.ValueChanged -= Gold_ValueChanged;
}
