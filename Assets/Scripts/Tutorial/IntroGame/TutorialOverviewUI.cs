using Network.Types;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Dear future me: STFU about duplication and fix this instead. I have a serious time constraint.
public class TutorialOverviewUI : MonoBehaviour
{
    TextMeshProUGUI myNameText;
    TextMeshProUGUI myLevelText;
    TextMeshProUGUI myScoreText;
    TextMeshProUGUI theirNameText;
    TextMeshProUGUI theirLevelText;
    TextMeshProUGUI theirScoreText;

    AvatarDisplay myAvatar;
    AvatarDisplay theirAvatar;

    Transform roundsContainer;
    GameObject roundTemplate;

    GameObject playButton;
    GameObject waitingForOpponentText;
    GameObject gameExpiredForMeText;
    GameObject gameExpiredForThemText;
    GameObject gameFinishedText;

    public TutorialRoundDetailsUI RoundDetails { get; private set; }

    public bool IsVisible => gameObject.activeInHierarchy;

    void Awake()
    {
        myNameText = transform.Find("MyProfile/Name").GetComponent<TextMeshProUGUI>();
        myLevelText = transform.Find("MyProfile/Level/Text").GetComponent<TextMeshProUGUI>();
        theirNameText = transform.Find("TheirProfile/Name").GetComponent<TextMeshProUGUI>();
        theirLevelText = transform.Find("TheirProfile/Level/Text").GetComponent<TextMeshProUGUI>();

        myScoreText = transform.Find("Score/MyScore").GetComponent<TextMeshProUGUI>();
        theirScoreText = transform.Find("Score/TheirScore").GetComponent<TextMeshProUGUI>();

        myAvatar = transform.Find("MyProfile/Avatar").GetComponent<AvatarDisplay>();
        theirAvatar = transform.Find("TheirProfile/Avatar").GetComponent<AvatarDisplay>();

        roundsContainer = transform.Find("Rounds");
        roundTemplate = roundsContainer.Find("Template").gameObject;

        playButton = transform.Find("PlayButton").gameObject;
        waitingForOpponentText = transform.Find("WaitingForOpponentText").gameObject;
        gameFinishedText = transform.Find("GameFinishedText").gameObject;
        gameExpiredForMeText = transform.Find("GameExpiredForMeText").gameObject;
        gameExpiredForThemText = transform.Find("GameExpiredForThemText").gameObject;

        RoundDetails = transform.Find("RoundDetails").GetComponent<TutorialRoundDetailsUI>();
        RoundDetails.Hide();

        Hide();
    }

    public void ShowGame(string theirName, uint theirLevel, uint myScore, uint theirScore,
        IEnumerable<RoundDisplayInfo> rounds, bool gameFinished, bool gameExpired, bool expiredForMe)
    {
        gameObject.SetActive(true);

        Translation.SetTextNoTranslate(myNameText, TransientData.Instance.UserName);
        Translation.SetTextNoTranslate(myLevelText, TransientData.Instance.Level.ToString());
        Translation.SetTextNoTranslate(myScoreText, myScore.ToString());

        Translation.SetTextNoTranslate(theirNameText, theirName);
        Translation.SetTextNoTranslate(theirLevelText, theirLevel.ToString());
        Translation.SetTextNoTranslate(theirScoreText, theirScore.ToString());

        myAvatar.SetAvatar(TransientData.Instance.Avatar);
        theirAvatar.SetAvatar(new AvatarDTO(new List<AvatarPartDTO>() 
        {
            new AvatarPartDTO(AvatarPartType.HeadShape, 1),
            new AvatarPartDTO(AvatarPartType.Eyes, 15),
            new AvatarPartDTO(AvatarPartType.Mouth, 6),
            new AvatarPartDTO(AvatarPartType.Hair, 24),
        }));

        roundsContainer.ClearContainer();

        var lastRound = default(RoundDisplayInfo);
        foreach (var round in rounds)
        {
            var tr = roundsContainer.AddListItem(roundTemplate);

            Translation.SetTextNoTranslate(tr.Find("MyScore/Text").GetComponent<TextMeshProUGUI>(), round.myScore?.ToString() ?? "؟");
            Translation.SetTextNoTranslate(tr.Find("TheirScore/Text").GetComponent<TextMeshProUGUI>(), round.theirScore?.ToString() ?? "؟");
            Translation.SetTextNoTranslate(tr.Find("Subject/Text").GetComponent<TextMeshProUGUI>(), round.category ?? "؟؟؟");

            tr.GetComponent<Button>().interactable = false;

            lastRound = round;
        }

        playButton.SetActive(false);
        waitingForOpponentText.SetActive(false);
        gameFinishedText.SetActive(false);
        gameExpiredForMeText.SetActive(false);
        gameExpiredForThemText.SetActive(false);

        if (gameFinished)
            gameFinishedText.SetActive(true);
        else if (gameExpired)
        {
            if (expiredForMe)
                gameExpiredForMeText.SetActive(true);
            else
                gameExpiredForThemText.SetActive(true);
        }
        else if (lastRound != null)
        {
            if (lastRound.state == RoundState.WaitingForMe)
                playButton.SetActive(true);
            else if (lastRound.state == RoundState.WaitingForThem)
                waitingForOpponentText.SetActive(true);
        }
    }

    public void ShowRoundDetails(RoundDisplayInfo round) => RoundDetails.Show(round);

    public void Hide() => gameObject.SetActive(false);
}
