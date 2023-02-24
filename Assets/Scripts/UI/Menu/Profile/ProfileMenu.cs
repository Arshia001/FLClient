using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class ProfileMenu : MonoBehaviour, INonGameMenu
{
    TextMeshProUGUI numGames, numWins, numLosses, numDraws, highestScore;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        TaskExtensions.RunIgnoreAsync(MenuManager.Instance.Show<MainMenu>);
        return true;
    });

    void Awake()
    {
        numGames = transform.Find("Statistics/GamesPlayed/Number").GetComponent<TextMeshProUGUI>();
        numWins = transform.Find("Statistics/GameResults/Win/Number").GetComponent<TextMeshProUGUI>();
        numLosses = transform.Find("Statistics/GameResults/Loss/Number").GetComponent<TextMeshProUGUI>();
        numDraws = transform.Find("Statistics/GameResults/Draw/Number").GetComponent<TextMeshProUGUI>();
        highestScore = transform.Find("Statistics/HighestScore/Number").GetComponent<TextMeshProUGUI>();
    }

    public Task Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);

        return Task.CompletedTask;
    }

    public void Show()
    {
        backStackHandler.MenuShown();

        gameObject.SetActive(true);
        Footer.Instance.SetActivePage(Footer.FooterIconType.Profile);

        var td = TransientData.Instance;
        var wins = td.GetStatisticValue(Statistics.GamesWon, 0);
        var losses = td.GetStatisticValue(Statistics.GamesLost, 0);
        var draws = td.GetStatisticValue(Statistics.GamesEndedInDraw, 0);

        Translation.SetTextNoTranslate(numGames, (wins + draws + losses).ToString());
        Translation.SetTextNoTranslate(numWins, wins.ToString());
        Translation.SetTextNoTranslate(numLosses, losses.ToString());
        Translation.SetTextNoTranslate(numDraws, draws.ToString());
        Translation.SetTextNoTranslate(highestScore, td.GetStatisticValue(Statistics.BestRoundScore, 0).ToString());
    }

    public void ShowFriends() => InformationToast.Instance.Enqueue("لیست دوستان به زودی به بازی اضافه می‌شه!");
}
