using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectLoginMethodStartup : MonoBehaviour
{
    public void Show() => gameObject.SetActive(true);

    public void Hide() => gameObject.SetActive(false);

    public void NewAccount() => transform.root.GetComponent<Startup>().SetLoginMethodNewAccount();

    public void EmailAccount() => transform.root.GetComponent<Startup>().SetLoginMethodLoginWithEmail();

    public void BazaarLogin()
    {
        var startup = transform.root.GetComponent<Startup>();
        var bgp = startup.InitializeBazaarGamesPlatform();
        bgp.Authenticate(response =>
        {
            if (!response)
            {
                InformationToast.Instance.Enqueue("خطا در ورود به حساب کافه‌بازار");
                return;
            }

            TaskExtensions.RunIgnoreAsync(async () =>
            {
                try
                {
                    using (LoadingIndicator.Show(true))
                    {
                        var result = await Startup.Instance.Connect(Network.Types.HandShakeMode.BazaarToken, null, null, null, bgp.GetUserId());
                        DataStore.Instance.HaveBazaarToken.Value = true;
                        Startup.Instance.SetLoginResult(result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    InformationToast.Instance.Enqueue("اتصال به سرور با خطا مواجه شد.");
                }
            });
        });
    }
}
