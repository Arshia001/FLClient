#pragma warning disable IDE0044 // make field readonly - non-readonly fields needed for serialization
#pragma warning disable IDE0040 // usused field - need to keep publicKey field on the object at all times since it's serialized

#if UNITY_ANDROID && !UNITY_EDITOR
#define WITH_BAZAAR
#endif

using CafeBazaar.Billing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum BazaarResultCode
{
    Success = 0,
    InvalidConsumption = 1,
    SubscriptionsNotAvailable = 2,
    UserCancelled = 3,
    ItemUnavailable = 4,
    ItemNotOwned = 5,
    ItemAlreadyOwned = 6,
    Error = 7,
    DeveloperError = 8,
    BillingUnavailable = 9,
    ExpectionInBillingSetup = 10
}

public class SkuDetails
{
    public string Title { get; }
    public string Description { get; }
    public ProductType Type { get; }
    public int Price { get; }
    public string Sku { get; }

    public SkuDetails(Product product)
    {
        Title = product.Title;
        Description = product.Description;
        Type = product.Type;
        Price = ConvertPriceToInt(product.Price);
        Sku = product.ProductId;
    }

    static int ConvertPriceToInt(string Price)
    {
        // There are two sets of "perso-arabic" digits in unicode, one is Persian and the other is Arabic. I have no idea which one Bazaar returns, so we check for both.
        Price = string.Concat(Price.Where(ch => (0x0660 <= ch && ch <= 0x0669) || (0x06f0 <= ch && ch <= 0x06f9)));
        if (Price.Length == 0)
            return 0;
        Price = Price
            .Replace((char)0x0660, '0')
            .Replace((char)0x0661, '1')
            .Replace((char)0x0662, '2')
            .Replace((char)0x0663, '3')
            .Replace((char)0x0664, '4')
            .Replace((char)0x0665, '5')
            .Replace((char)0x0666, '6')
            .Replace((char)0x0667, '7')
            .Replace((char)0x0668, '8')
            .Replace((char)0x0669, '9')
            .Replace((char)0x06f0, '0')
            .Replace((char)0x06f1, '1')
            .Replace((char)0x06f2, '2')
            .Replace((char)0x06f3, '3')
            .Replace((char)0x06f4, '4')
            .Replace((char)0x06f5, '5')
            .Replace((char)0x06f6, '6')
            .Replace((char)0x06f7, '7')
            .Replace((char)0x06f8, '8')
            .Replace((char)0x06f9, '9');

        return int.Parse(Price);
    }
}

public static class BazaarIabManager
{
    static BazaarResponse<U> MapBazaarResponse<T, U>(BazaarResponse<T> response, Func<T, U> transform) =>
        response.Successful ?
        BazaarResponse<U>.Success(transform(response.Body)) :
        BazaarResponse<U>.Error(response.Message);

    public static Task<BazaarResponse> Initialize()
    {
        var tcs = new TaskCompletionSource<BazaarResponse>();
        BazaarBilling.Init(response => tcs.TrySetResult(response));
        return tcs.Task;
    }

    public static Task<BazaarResponse<(BazaarResultCode code, Purchase purchase)>> Purchase(string productSku, string payload = "")
    {
        BazaarResultCode ParseResultCode(string message)
        {
            // The response message comes in this format: "<CODE>::<MESSAGE>"
            // ... and it only comes in this specific method's response.
            var idx = message?.IndexOf("::");
            if (!idx.HasValue || idx.Value < 0)
                return BazaarResultCode.Error;

            if (int.TryParse(message.Substring(0, idx.Value), out var code))
                return (BazaarResultCode)code;

            return BazaarResultCode.Error;
        }

        var tcs = new TaskCompletionSource<BazaarResponse<(BazaarResultCode code, Purchase purchase)>>();
        BazaarBilling.Purchase(productSku, payload, response =>
        {
            if (response.Successful)
                tcs.TrySetResult(BazaarResponse<(BazaarResultCode code, Purchase purchase)>.Success((BazaarResultCode.Success, response.Body)));
            else
            {
                var code = ParseResultCode(response.Message);
                tcs.TrySetResult(new BazaarResponse<(BazaarResultCode code, Purchase purchase)>(response.Message, (code, null), false));
            }
        });
        return tcs.Task;
    }

    public static Task<BazaarResponse<Purchase>> Consume(string productSku)
    {
        var tcs = new TaskCompletionSource<BazaarResponse<Purchase>>();
        BazaarBilling.Consume(productSku, response => tcs.TrySetResult(response));
        return tcs.Task;
    }

    public static Task<BazaarResponse<(bool userHasItem, Purchase purchaseData)>> CheckHasItem(string productSku)
    {
        var tcs = new TaskCompletionSource<BazaarResponse<(bool userHasItem, Purchase purchaseData)>>();
        BazaarBilling.GetInventory(new[] { productSku }, response => tcs.TrySetResult(MapBazaarResponse(response, inventory =>
        {
            var purchase = inventory.Purchases.FirstOrDefault(p => p.ProductId == productSku);
            return (purchase != null, purchase);
        })));
        return tcs.Task;
    }

    public static Task<BazaarResponse<Dictionary<string, Purchase>>> GetPurchases()
    {
        var tcs = new TaskCompletionSource<BazaarResponse<Dictionary<string, Purchase>>>();
        BazaarBilling.GetPurchases(response => tcs.TrySetResult(MapBazaarResponse(response, purchases => purchases.ToDictionary(p => p.ProductId))));
        return tcs.Task;
    }

    public static Task<BazaarResponse<Dictionary<string, SkuDetails>>> GetSkuDetails(IEnumerable<string> skus)
    {
        var tcs = new TaskCompletionSource<BazaarResponse<Dictionary<string, SkuDetails>>>();
        BazaarBilling.GetSkuDetails(skus.ToArray(), response =>
            tcs.TrySetResult(MapBazaarResponse(response, skuDetails => skuDetails.ToDictionary(p => p.ProductId, p => new SkuDetails(p))))
        );
        return tcs.Task;
    }
}
