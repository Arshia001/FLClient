using CafeBazaar.Billing;
using GameAnalyticsSDK;
using Network;
using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class IabManager : SingletonBehaviour<IabManager>
{
    Dictionary<string, (GoldPackConfigDTO pack, uint price)> goldPacks;

    protected override bool IsGlobal => true;

    public IEnumerable<(GoldPackConfigDTO pack, uint price)> GoldPacks => goldPacks?.Values;

    public event Action PricesAvailable;

    // This doesn't need to run before we can launch the game
    public void Initialize(IEnumerable<GoldPackConfigDTO> goldPacks) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var initResponse = await BazaarIabManager.Initialize();
        if (!initResponse.Successful)
        {
            Debug.LogError("Failed to initialize Bazaar IAB: " + initResponse.Message);
            return;
        }

        Dictionary<string, SkuDetails> details;

        var delay = 1000.0f;
        while (true)
        {
            var detailsResponse = await BazaarIabManager.GetSkuDetails(goldPacks.Select(g => g.Sku));

            if (detailsResponse.Successful)
            {
                details = detailsResponse.Body;
                break;
            }
            else
            {
#if UNITY_EDITOR
                this.goldPacks = goldPacks.ToDictionary(g => g.Sku, g => (g, 10000u));
#endif

                Debug.LogError($"Failed to get SKU details due to {detailsResponse.Message}, will retry");
                await Task.Delay((int)delay);
                delay *= 1.2f;
                continue;
            }

        }

        this.goldPacks =
            goldPacks
            .Where(g => details.ContainsKey(g.Sku))
            .ToDictionary(g => g.Sku, g => (g, (uint)details[g.Sku].Price));

        PricesAvailable?.Invoke();

        Dictionary<string, Purchase> inventory;
        delay = 1000.0f;
        while (true)
        {
            var inventoryResponse = await BazaarIabManager.GetPurchases();
            if (inventoryResponse.Successful)
            {
                inventory = inventoryResponse.Body;
                break;
            }
            else
            {
                Debug.LogError($"Failed to get inventory due to {inventoryResponse.Message}, will retry");
                await Task.Delay((int)delay);
                delay *= 1.2f;
                continue;
            }
        }

        Debug.Log($"Have {inventory?.Count} existing purchases in inventory");

        if (inventory != null)
            foreach (var o in inventory)
            {
                Debug.Log($"Processing existing purchase of {o.Value.ProductId}");

                var (result, totalGold) = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().BuyGoldPack(o.Value.ProductId, o.Value.PurchaseToken);

                if (result == IabPurchaseResult.Success || result == IabPurchaseResult.AlreadyProcessed)
                {
                    TransientData.Instance.Gold.Value = totalGold;

                    SoundEffectManager.Play(SoundEffect.GainCoins);

                    var consumeResponse = await BazaarIabManager.Consume(o.Value.ProductId);
                    if (!consumeResponse.Successful)
                        Debug.LogError($"Failed to consume purchased product {o.Value.ProductId} due to {consumeResponse.Message}");
                }

                if (result == IabPurchaseResult.Success)
                    InformationToast.ShowWhenAvailable("خریدی که قبلا انجام داده بودی به حسابت لحاظ شد. ببخش که طول کشید!");
            }
    });

    public async Task<(BazaarResultCode, IabPurchaseResult?)> MakePurchase(string sku)
    {
        var pack = goldPacks[sku];

        ConnectionManager.Instance.DelayKeepAlive(TimeSpan.FromMinutes(20));

        var purchaseResponse = await BazaarIabManager.Purchase(sku);

        ConnectionManager.Instance.ResetKeepAlive();

        if (!purchaseResponse.Successful && purchaseResponse.Body.code != BazaarResultCode.ItemAlreadyOwned)
            return (purchaseResponse.Body.code, null);

        if (purchaseResponse.Body.code == BazaarResultCode.ItemAlreadyOwned)
        {
            var inv = await BazaarIabManager.GetPurchases();

            if (!inv.Successful)
                return (BazaarResultCode.Error, null);

            if (inv.Body.TryGetValue(sku, out var purchase))
                purchaseResponse = BazaarResponse<(BazaarResultCode, Purchase)>.Success((BazaarResultCode.Success, purchase));
            else
            {
                Debug.LogError($"Purchase reported having item in inventory but GetInventory failed to find it: {sku}");
                return (BazaarResultCode.Error, null);
            }
        }
        else
        {
            // Successful purchase
            GameAnalytics.NewBusinessEvent("IRR", (int)pack.price, "GoldPack", sku, "Shop");
            GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "gold", pack.pack.NumGold, "IAB", sku);
        }

        var (result, totalGold) = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().BuyGoldPack(sku, purchaseResponse.Body.purchase.PurchaseToken);

        if (result == IabPurchaseResult.Success || result == IabPurchaseResult.AlreadyProcessed)
        {
            TransientData.Instance.Gold.Value = totalGold;
            SoundEffectManager.Play(SoundEffect.GainCoins);

            var consumeResponse = await BazaarIabManager.Consume(sku);
            if (!consumeResponse.Successful)
                // We'll just consume it the next time around
                Debug.LogError($"Failed to consume purchased product {sku} due to {consumeResponse.Message}");
        }

        return (purchaseResponse.Body.code, result);
    }
}
