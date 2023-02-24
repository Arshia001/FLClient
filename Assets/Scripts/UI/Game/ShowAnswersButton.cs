using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShowAnswersButton : MonoBehaviour
{
    TextMeshProUGUI text, priceText;

    bool showingAnswers, haveAnswers;

    void Awake()
    {
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        priceText = transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
    }

    void Start() => Refresh();

    void Update() => Refresh();

    public void SetText(bool showingAnswers, bool haveAnswers)
    {
        this.showingAnswers = showingAnswers;
        this.haveAnswers = haveAnswers;
        Refresh();
    }

    void Refresh()
    {
        text.alignment = showingAnswers || haveAnswers ? TextAlignmentOptions.Center : TextAlignmentOptions.Right;
        Translation.SetText(text, showingAnswers ? "ShowOwnAnswers" : "ShowAllAnswers");

        if (showingAnswers || haveAnswers)
            priceText.gameObject.SetActive(false);
        else
        {
            priceText.gameObject.SetActive(true);
            if (AdRepository.Instance.IsAdAvailable(AdRepository.AdZone.GetCategoryAnswers))
                Translation.SetTextNoShape(priceText, MoneySprites.GiftBox + " " +
                    PersianTextShaper.PersianTextShaper.ShapeText("مجانی!"));
            else
                Translation.SetTextNoShape(priceText, MoneySprites.SingleCoin + " " +
                    PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.ConfigValues.GetAnswersPrice.ToString()));
        }
    }
}
