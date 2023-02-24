using System;
using System.Collections;
using System.Collections.Generic;
using Network.Types;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static GameRepository;

public class GameButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Color winColor = default, lossColor = default, drawColor = default, claimRewardColor = default;
    [SerializeField] Sprite normalFrame = default, claimRewardFrame = default;

    TextMeshProUGUI remainingTimeText;
    DateTime? expiryTime;
    bool refreshRequestedForReachingExpiryTime;

    public Guid gameID { get; set; }

    public void OnPointerClick(PointerEventData eventData)
    {
        MenuManager.Instance.Menu<MainMenu>().GameSelected(gameID);
    }

    static string GetScoreText(SimplifiedGameInfo game)
    {
        string shape(string s) => PersianTextShaper.PersianTextShaper.ShapeText(s, rightToLeftRenderDirection: true);

        if (game.GameState == GameState.Expired)
            return shape(game.WinnerOfExpiredGame ? "وقت حریف تموم شد" : "وقتت تموم شد");
        else
            return $"{shape("من")} {shape(game.MyScore.ToString())} - {shape(game.TheirScore.ToString())} {shape("حریف")}";
    }

    void Update() => UpdateRemainingTime();

    void UpdateRemainingTime()
    {
        if (expiryTime.HasValue)
        {
            remainingTimeText.gameObject.SetActive(true);
            Translation.SetTextNoShape(remainingTimeText, (expiryTime.Value < DateTime.Now ? TimeSpan.Zero : expiryTime.Value - DateTime.Now).FormatAsClock());

            if (!refreshRequestedForReachingExpiryTime && expiryTime.Value < DateTime.Now)
            {
                // TODO There is much room for improvement here, since we already know everything about the
                // game. However, this case is rare enough that I don't really care about the added network
                // call here. Let's fix it later.
                refreshRequestedForReachingExpiryTime = true;
                MenuManager.Instance.Menu<MainMenu>().RefreshGames();
            }
        }
        else
            remainingTimeText.gameObject.SetActive(false);
    }

    public void SetGameInfo(SimplifiedGameInfo game)
    {
        gameID = game.GameID;

        var tr = transform;

        Translation.SetTextNoTranslate(tr.Find("Texts/NameAndStatus/OpponentName").GetComponent<TextMeshProUGUI>(), game.OtherPlayerName ?? "حریف شانسی");

        var statusText = tr.Find("Texts/NameAndStatus/StatusText").GetComponent<TextMeshProUGUI>();
        Translation.SetTextNoShape(statusText, GetScoreText(game));
        statusText.isRightToLeftText = true;

        var resultText = tr.Find("Texts/ResultText").GetComponent<TextMeshProUGUI>();
        if (game.GameState.GameHasEnded())
        {
            resultText.gameObject.SetActive(true);
            var winner = game.GameState == GameState.Expired ? game.WinnerOfExpiredGame : game.MyScore > game.TheirScore;
            var draw = game.GameState == GameState.Finished && game.MyScore == game.TheirScore;
            Translation.SetTextNoTranslate(resultText, game.RewardPending ? "جایزتو بردار" : winner ? "بردی" : draw ? "برابر" : "باختی");
            resultText.color = game.RewardPending ? claimRewardColor : winner ? winColor : draw ? drawColor : lossColor;
            transform.Find("Frame").GetComponent<Image>().sprite = game.RewardPending ? claimRewardFrame : normalFrame;
        }
        else
        {
            resultText.gameObject.SetActive(false);
            transform.Find("Frame").GetComponent<Image>().sprite = normalFrame;
        }

        remainingTimeText = tr.Find("TimeText").GetComponent<TextMeshProUGUI>();
        expiryTime = game.ExpiryTime;
        refreshRequestedForReachingExpiryTime = false;
        UpdateRemainingTime();

        tr.Find("AvatarPlaceholder").gameObject.SetActive(game.OtherPlayerAvatar == null);
        tr.Find("Avatar").GetComponent<AvatarDisplay>().SetAvatar(game.OtherPlayerAvatar);
    }
}
