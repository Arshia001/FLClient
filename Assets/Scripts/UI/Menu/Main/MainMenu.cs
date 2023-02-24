using GameAnalyticsSDK;
using Network;
using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static GameRepository;

public class MainMenu : MonoBehaviour, INonGameMenu
{
    public event Action Refresh;

    Transform gamesContainer;
    GameObject headerTemplate;
    GameObject finishedGamesHeaderTemplate;
    GameObject gameTemplate;

    SubMenuGroupWithContainer<MainMenuSubMenu> subMenus;

    LevelUpNotification levelUpNotification;
    uint? pendingLevelUpNotification;

    void Awake()
    {
        gamesContainer = transform.Find("List/Viewport/Content/GamesList");
        headerTemplate = gamesContainer.Find("HeaderTemplate").gameObject;
        finishedGamesHeaderTemplate = gamesContainer.Find("FinishedGamesHeaderTemplate").gameObject;
        gameTemplate = gamesContainer.Find("GameTemplate").gameObject;

        subMenus = new SubMenuGroupWithContainer<MainMenuSubMenu>(transform.Find("SubMenus").gameObject);

        //!! this needs to be optimized to only update the entry for the updated game
        GameManager.Instance.GameUpdated += OnGameUpdated;

        levelUpNotification = transform.Find("LevelUpNotification").GetComponent<LevelUpNotification>();
        levelUpNotification.Hidden += LevelUpNotification_Hidden;
        TransientData.Instance.Level.ValueChanged += Level_ValueChanged;

        RefreshMenu();
    }

    void OnDestroy()
    {
        GameManager.Instance.GameUpdated -= OnGameUpdated;
        levelUpNotification.Hidden -= LevelUpNotification_Hidden;
        TransientData.Instance.Level.ValueChanged -= Level_ValueChanged;
    }

    void OnGameUpdated(Guid _id) => RefreshMenu();

    void ShowCoinGifts() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var ep = ConnectionManager.Instance.EndPoint<SystemEndPoint>();
        var td = TransientData.Instance;

        while (td.CoinGifts.Count > 0)
        {
            var gift = td.CoinGifts.Last();
            var text = GetGiftDescription(gift);
            await DialogBox.Instance.Show(text, "بده بیاد!");

            ulong? gold;
            using (LoadingIndicator.Show(true))
                gold = await ep.ClaimCoinGift(gift.GiftID);

            if (gold.HasValue)
            {
                GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "gold", gold.Value - td.Gold.Value, "gift", gift.Subject.ToString().CapLength(16));

                td.Gold.Value = gold.Value;

                SoundEffectManager.Play(SoundEffect.GainCoins);
            }
            else
            {
                await DialogBox.Instance.Show("دریافت هدیه با خطا مواجه شد. ممکنه زمان دریافت هدیه تموم شده باشه.", "خب");
            }

            td.CoinGifts.RemoveAt(td.CoinGifts.Count - 1);
        }
    });

    string GetGiftDescription(CoinGiftInfoDTO gift)
    {
        switch (gift.Subject)
        {
            case CoinGiftSubject.GiftToAll:
                return $"{gift.Description}\n{gift.Count} سکه هدیه گرفتی!";

            case CoinGiftSubject.SuggestedWords:
                {
                    var words = gift.ExtraData2?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    var result = $"به خاطر کلمه‌هایی که برای موضوع {gift.ExtraData1} پیشنهاد داده بودی، {gift.Count} سکه هدیه گرفتی!";
                    if (words.Any())
                        result += $"\nکلمه‌های تایید شده: {string.Join("، ", words)}";
                    return result;
                }

            case CoinGiftSubject.SuggestedCategories:
                return $"موضوع {gift.ExtraData1} که پیشنهاد داده بودی، تایید شده و به خاطرش {gift.Count} سکه هدیه گرفتی!";

            case CoinGiftSubject.FriendInvited:
                return $"دوستت {gift.ExtraData1} که به بازی دعوت کرده بودی ثبت نامشو کامل کرد و به خاطرش {gift.Count} سکه هدیه گرفتی!";

            default:
                return string.Empty;
        }
    }

    void ShowLevelUpNotification(uint level)
    {
        levelUpNotification.Show(level);
    }

    void LevelUpNotification_Hidden() => TutorialManager.Instance.BackToMainMenu();

    void Level_ValueChanged(uint newValue)
    {
        if (gameObject.activeSelf)
            ShowLevelUpNotification(newValue);
        else
            pendingLevelUpNotification = newValue;
    }

    bool ShowLevelUpIfPending()
    {
        if (pendingLevelUpNotification.HasValue)
        {
            ShowLevelUpNotification(pendingLevelUpNotification.Value);
            pendingLevelUpNotification = null;
            return true;
        }

        var td = TransientData.Instance;
        if (td.NotifiedLevel.Value != td.Level.Value)
        {
            ShowLevelUpNotification(td.Level);
            return true;
        }

        return false;
    }

    public void Show() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        gameObject.SetActive(true);
        TaskExtensions.RunIgnoreAsync(subMenus.HideAll);
        Footer.Instance.SetActivePage(Footer.FooterIconType.Home);
        RefreshMenu();

        if (await GameManager.Instance.ResumeInProgressGameIfAny())
        {
            MenuManager.Instance.HideAll();
            return;
        }

        if (ShowLevelUpIfPending())
            return;

        if (TutorialManager.Instance.BackToMainMenu())
            return;

        ShowCoinGifts();
    });

    public Task Hide()
    {
        gameObject.SetActive(false);

        return Task.CompletedTask;
    }

    public void HideSubMenus() => TaskExtensions.RunIgnoreAsync(subMenus.HideAll);

    public void ShowSubMenu<T>() where T : MainMenuSubMenu => TaskExtensions.RunIgnoreAsync(subMenus.Show<T>);

    public void GameSelected(Guid gameID)
    {
        if (GameRepository.Instance.GetSimplifiedGameInfo(gameID)?.RewardPending ?? false)
            TaskExtensions.RunIgnoreAsync(async () =>
            {
                using (LoadingIndicator.Show(true))
                    if (await GameManager.Instance.ClaimReward(gameID))
                        RefreshMenu();
            });
        else
        {
            MenuManager.Instance.HideAll();
            GameManager.Instance.ContinueGame(gameID);
        }
    }

    public void StartNewGame()
    {
        var td = TransientData.Instance;
        var upgradedActiveGameLimitActive = td.UpgradedActiveGameLimitEndTime.Value.HasValue && td.UpgradedActiveGameLimitEndTime.Value.Value > DateTime.Now;
        var activeGameCount = GameRepository.Instance.SimpleGameInfoes.Where(g => g.GameState != GameState.Finished && g.GameState != GameState.Expired).Count();
        var activeGameLimit = upgradedActiveGameLimitActive ? td.ConfigValues.MaxActiveGamesWhenUpgraded : td.ConfigValues.MaxActiveGames;
        if (activeGameCount < activeGameLimit)
        {
            MenuManager.Instance.HideAll();
            GameManager.Instance.StartNew();
        }
        else
        {
            if (upgradedActiveGameLimitActive)
                InformationToast.Instance.Enqueue("تعداد بازی‌های فعالت پر شده. صبر کن تا حریفات بازی کنن.");
            else
            {
                if (!TutorialManager.Instance.NeedToActivateUpgradedActiveGameLimitMode())
                    InformationToast.Instance.Enqueue("تعداد بازی‌های فعالت پر شده. اگه بخوای می‌تونی افزایش تعداد بازی رو فعال کنی.");
            }
        }
    }

    public void ActivateUpgradedActiveGameLimit()
    {
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            var td = TransientData.Instance;

            if (td.UpgradedActiveGameLimitEndTime > DateTime.Now)
            {
                InformationToast.Instance.Enqueue("قبلا افزایش تعداد بازی رو فعال کردی.");
                return;
            }

            if (td.Gold < td.ConfigValues.UpgradedActiveGameLimitPrice)
            {
                if (await DialogBox.Instance.Show($"برای افزایش تعداد بازی فعال {td.ConfigValues.UpgradedActiveGameLimitPrice - td.Gold.Value} سکه کم داری.‌ می‌خوای بریم فروشگاه؟", "بریم", "بی‌خیال") == DialogBox.Result.Yes)
                    MenuManager.Instance.ShowShop();
                return;
            }

            if ((await DialogBox.Instance.Show("می‌خوای تعداد بازی‌هات رو افزایش بدی؟", "آره", "نه")) == DialogBox.Result.No)
                return;

            using (LoadingIndicator.Show(true))
            {
                var (success, totalGold, duration) = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().ActivateUpgradedActiveGameLimit();
                if (success)
                {
                    using (ChangeNotifier.BeginBatch())
                    {
                        GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "gold", td.Gold.Value - totalGold, "powerup", "MaxActiveGames");
                        td.Gold.Value = totalGold;
                        td.UpgradedActiveGameLimitEndTime.Value = DateTime.Now + duration;
                    }
                }
                else if (duration > TimeSpan.Zero) // out of sync with server, still have time remaining
                {
                    td.UpgradedActiveGameLimitEndTime.Value = DateTime.Now + duration;
                }
            }
        });
    }

    void RefreshMenu()
    {
        var td = TransientData.Instance;

        gamesContainer.ClearContainer();

        var games = GameRepository.Instance.SimpleGameInfoes;
        CreateGameGroup("نوبت من", games.Where(g => !g.GameState.GameHasEnded() && g.MyTurn), false);
        CreateGameGroup("نوبت حریف", games.Where(g => !g.GameState.GameHasEnded() && !g.MyTurn), false);
        CreateGameGroup("تمام شده", games.Where(g => g.GameState.GameHasEnded()), true);

        Refresh?.Invoke();
    }

    void CreateGameGroup(string title, IEnumerable<SimplifiedGameInfo> games, bool isFinishedGames)
    {
        if (!games.Any())
            return;

        var header = gamesContainer.AddListItem(isFinishedGames ? finishedGamesHeaderTemplate : headerTemplate);
        Translation.SetTextNoTranslate(header.Find("Text").GetComponent<TextMeshProUGUI>(), title);

        foreach (var game in games)
            gamesContainer.AddListItem(gameTemplate)
                .GetComponent<GameButton>()
                .SetGameInfo(game);
    }

    public void RefreshGames() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        using (LoadingIndicator.Show(true))
            await GameRepository.Instance.RefreshSimpleInfoes();

        MainThreadDispatcher.Instance.Enqueue(() => RefreshMenu());
    });

    public void RemoveOldGames() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        using (LoadingIndicator.Show(true))
            await GameRepository.Instance.RemoveOldGames();

        MainThreadDispatcher.Instance.Enqueue(() => RefreshMenu());
    });

    public void GiftTapped() => ShowSubMenu<GiftListMainMenuSubMenu>();
}
