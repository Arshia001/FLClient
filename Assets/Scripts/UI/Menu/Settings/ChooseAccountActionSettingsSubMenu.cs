using System.Collections;
using System.Collections.Generic;
using CafeBazaar.Games;
using CafeBazaar.Games.BasicApi;
using Network;
using Network.Types;
using UnityEngine;

public class ChooseAccountActionSettingsSubMenu : SettingsSubMenu
{
    public void Login() => SettingsMenu.Instance.Show<LoginSettingsSubMenu>();

    public void CreateAccount() => SettingsMenu.Instance.Show<CreateAccountSettingsSubMenu>();

    BazaarGamesPlatform InitializeBazaarGamesPlatform()
    {
        var config = new BazaarGamesClientConfiguration.Builder().Build();
        BazaarGamesPlatform.InitializeInstance(config);
        return BazaarGamesPlatform.Activate();
    }

    public void LoginWithBazaar()
    {
        var bgp = InitializeBazaarGamesPlatform();
        bgp.Authenticate(response =>
        {
            if (!response)
            {
                InformationToast.Instance.Enqueue("خطا در ورود به حساب کافه‌بازار");
                return;
            }

            TaskExtensions.RunIgnoreAsync(async () =>
            {
                BazaarRegistrationResult result;

                using (LoadingIndicator.Show(true))
                {
                    result = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().PerformBazaarTokenRegistration(bgp.GetUserId());
                }

                switch (result)
                {
                    case BazaarRegistrationResult.Success:
                        {
                            DataStore.Instance.HaveBazaarToken.Value = true;

                            var td = TransientData.Instance;
                            td.RegistrationStatus.Value = RegistrationStatus.BazaarToken;

                            SettingsMenu.Instance.Show<EditBazaarAccountSettingsSubMenu>();
                            break;
                        }

                    case BazaarRegistrationResult.AccountWithTokenExists:
                        {
                            if (await DialogBox.Instance.Show("با این حساب کافه‌بازار قبلا یه حساب مدادجنگی ثبت شده. می‌تونی به اون حساب وارد شی، ولی حساب فعلیت از بین می‌ره. ادامه می‌دی؟", "آره", "نه") == DialogBox.Result.Yes)
                                SwitchToBazaarAccount();
                            break;
                        }

                    // The rest of these cases should *really* be impossible
                    case BazaarRegistrationResult.AlreadyRegisteredWithOtherMethod:
                    case BazaarRegistrationResult.AlreadyHaveOtherBazaarToken:
                        InformationToast.Instance.Enqueue("ثبت نام این حساب قبلا انجام شده؛ ثبت نام تکراری مجاز نیست.");
                        break;

                    case BazaarRegistrationResult.AlreadyHaveSameBazaarToken:
                        SwitchToBazaarAccount();
                        break;

                    default:
                        Debug.LogWarning("Unknown bazaar registration result: " + result.ToString());
                        break;
                }
            });
        });
    }

    void SwitchToBazaarAccount()
    {
        ProfileHelper.LogOut();
        DataStore.Instance.HaveBazaarToken.Value = true;
    }
}
