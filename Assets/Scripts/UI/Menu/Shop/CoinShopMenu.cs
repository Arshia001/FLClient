using Network.Types;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoinShopMenu : MonoBehaviour
{
    GameObject havePricesContainer;
    GameObject loadingPricesContainer;
    Transform packsContainer;

    void Awake()
    {
        havePricesContainer = transform.Find("HavePrices").gameObject;
        loadingPricesContainer = transform.Find("LoadingPrices").gameObject;

        packsContainer = havePricesContainer.transform.Find("Packs");

        IabManager.Instance.PricesAvailable += OnPricesAvailable;
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Show();
        else
            Hide();
    }

    private void Hide() => gameObject.SetActive(false);

    void Show()
    {
        gameObject.SetActive(true);
        RefreshUI();
    }

    void OnDestroy() => IabManager.Instance.PricesAvailable -= OnPricesAvailable;

    private void OnPricesAvailable()
    {
        if (isActiveAndEnabled)
            RefreshUI();
    }

    private void RefreshUI()
    {
        if (IabManager.Instance.GoldPacks != null)
        {
            loadingPricesContainer.SetActive(false);
            havePricesContainer.SetActive(true);

            var packs = IabManager.Instance.GoldPacks.OrderBy(p => p.pack.NumGold);
            var count = 0;
            foreach (var pack in packs)
            {
                var tr = packsContainer.Find(count.ToString());
                tr.gameObject.SetActive(true);
                tr.GetComponent<GoldPackButton>().Initialize(pack.pack.Sku, pack.pack.Title, (int)pack.price, (int)pack.pack.NumGold, pack.pack.Tag);
                ++count;
            }

            for (; count < 6; ++count)
                packsContainer.Find(count.ToString()).gameObject.SetActive(false);
        }
        else
        {
            loadingPricesContainer.SetActive(true);
            havePricesContainer.SetActive(false);
        }
    }

    public void MakePurchase(string sku) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        using (LoadingIndicator.Show(true))
        {
            var (bazaarResult, result) = await IabManager.Instance.MakePurchase(sku);

            if (bazaarResult == BazaarResultCode.Success && result.HasValue)
            {
                switch (result.Value)
                {
                    case IabPurchaseResult.Success:
                        InformationToast.Instance.Enqueue("خرید انجام شد، مبارک باشه!");
                        break;

                    case IabPurchaseResult.AlreadyProcessed:
                        InformationToast.Instance.Enqueue("این خرید قبلا به حسابت لحاظ شده.");
                        break;

                    case IabPurchaseResult.FailedToContactValidationService:
                    case IabPurchaseResult.UnknownError:
                        DialogBox.Instance.Show(
                            "ما نتونستیم خریدت رو اعتبارسنجی کنیم. اگه پول از حسابت کم شده باشه، دفعه بعد که بازی رو باز کنی سکه‌هاتو تحویل می‌گیری. شرمنده!"
                            , "باشه"
                            ).Ignore();
                        break;

                    case IabPurchaseResult.Invalid:
                        InformationToast.Instance.Enqueue("خرید انجام نشد.");
                        break;
                }
            }
            else
            {
                switch (bazaarResult)
                {
                    case BazaarResultCode.Error:
                    case BazaarResultCode.DeveloperError:
                        InformationToast.Instance.Enqueue("خطا در روند خرید");
                        break;

                    case BazaarResultCode.BillingUnavailable:
                    case BazaarResultCode.ExpectionInBillingSetup:
                        InformationToast.Instance.Enqueue("خطا در ارتباط با کافه‌بازار");
                        break;

                    case BazaarResultCode.UserCancelled:
                        InformationToast.Instance.Enqueue("پشیمون شدی؟");
                        break;

                    default:
                        InformationToast.Instance.Enqueue("خرید لغو شد");
                        break;
                }
            }
        }
    });
}
