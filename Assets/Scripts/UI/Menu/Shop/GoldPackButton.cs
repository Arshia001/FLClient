using System.Collections;
using System.Collections.Generic;
using Network.Types;
using TMPro;
using UnityEngine;

public class GoldPackButton : MonoBehaviour
{
    public string Sku { get; private set; }

    public void Initialize(string sku, string title, int price, int goldCount, GoldPackTag tag)
    {
        Sku = sku;
        Translation.SetTextNoTranslate(transform.Find("Title").GetComponent<TextMeshProUGUI>(), title);
        Translation.SetTextNoShape(transform.Find("Price").GetComponent<TextMeshProUGUI>(), StylizePrice(price));
        Translation.SetTextNoTranslate(transform.Find("Count").GetComponent<TextMeshProUGUI>(), goldCount.ToString());
        transform.Find("BestSelling").gameObject.SetActive(tag == GoldPackTag.BestSelling);
        transform.Find("BestValue").gameObject.SetActive(tag == GoldPackTag.BestValue);
    }

    string StylizePrice(int priceIRR)
    {
        if (priceIRR == 0)
            return PersianTextShaper.PersianTextShaper.ShapeText("مجانی");

        var shapedNumber = PersianTextShaper.PersianTextShaper.ShapeText((priceIRR / 10).ToString());
        var shapedToman = PersianTextShaper.PersianTextShaper.ShapeText("تومن");
        return $"<size=75%>{shapedToman}</size> {shapedNumber.Substring(0, shapedNumber.Length - 3)}<size=60%>,{shapedNumber.Substring(shapedNumber.Length - 3)}</size>";
    }

    public void OnClick() => MenuManager.Instance.Menu<ShopMenu>().CoinShop.MakePurchase(Sku);
}
