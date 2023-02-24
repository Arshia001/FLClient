using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProfileBox : MonoBehaviour
{
    TextMeshProUGUI username, score, rank, level;
    ProfileLevelDots levelDots;
    AvatarDisplay avatar;

    void Awake()
    {
        username = transform.Find("Name").GetComponent<TextMeshProUGUI>();
        score = transform.Find("Score").GetComponent<TextMeshProUGUI>();
        rank = transform.Find("Rank").GetComponent<TextMeshProUGUI>();
        level = transform.Find("Level/Text").GetComponent<TextMeshProUGUI>();
        levelDots = transform.Find("LevelDots").GetComponent<ProfileLevelDots>();
        avatar = transform.Find("Avatar").GetComponent<AvatarDisplay>();
    }

    void OnEnable()
    {
        var td = TransientData.Instance;
        Translation.SetTextNoTranslate(username, td.UserName);
        Translation.SetTextNoTranslate(score, td.Score.ToString());
        Translation.SetTextNoTranslate(rank, td.Rank.ToString());
        Translation.SetTextNoTranslate(level, td.Level.ToString());
        levelDots.Fill = td.XP.Value / (float)td.NextLevelXPThreshold.Value;
        avatar.SetAvatar(td.Avatar);
    }

    public void OnAvatarTapped() => MenuManager.Instance.ShowAvatarShop();
}
