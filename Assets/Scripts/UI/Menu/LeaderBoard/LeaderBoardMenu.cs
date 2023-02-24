using Network;
using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderBoardMenu : MonoBehaviour, INonGameMenu
{
    [SerializeField] Sprite activeButton = null, inactiveButton = null;

    [SerializeField] Sprite rank1st = null, rank2nd = null, rank3rd = null, rankOthers = null;

    readonly Dictionary<(LeaderBoardSubject, LeaderBoardGroup), IReadOnlyList<LeaderBoardEntryDTO>> entryCache
        = new Dictionary<(LeaderBoardSubject, LeaderBoardGroup), IReadOnlyList<LeaderBoardEntryDTO>>();

    RectTransform entriesList;
    GameObject entryTemplate, ownEntryProxyTemplate, discontinuityTemplate;

    Button xpButton, rankButton;
    Button allButton, friendsButton, groupButton;

    LeaderBoardOwnScoreEntry ownScoreEntry;

    IDisposable loadingIndicator;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() =>
    {
        TaskExtensions.RunIgnoreAsync(MenuManager.Instance.Show<MainMenu>);
        return true;
    });

    void Awake()
    {
        entriesList = transform.Find("Entries/Viewport/Content") as RectTransform;
        entryTemplate = entriesList.Find("Template").gameObject;
        ownEntryProxyTemplate = entriesList.Find("OwnEntryProxyTemplate").gameObject;
        discontinuityTemplate = entriesList.Find("DiscontinuityTemplate").gameObject;

        ownScoreEntry = transform.Find("Entries/Viewport/OwnEntry").GetComponent<LeaderBoardOwnScoreEntry>();

        xpButton = transform.Find("Buttons/Subject/XP").GetComponent<Button>();
        rankButton = transform.Find("Buttons/Subject/Rank").GetComponent<Button>();

        allButton = transform.Find("Buttons/Group/All").GetComponent<Button>();
        friendsButton = transform.Find("Buttons/Group/Friends").GetComponent<Button>();
        groupButton = transform.Find("Buttons/Group/Group").GetComponent<Button>();
    }

    public void Show()
    {
        gameObject.SetActive(true);

        backStackHandler.MenuShown();

        Footer.Instance.SetActivePage(Footer.FooterIconType.LeaderBoard);

        entryCache.Clear();
        ChangeLeaderBoard(LeaderBoardSubject.Score, LeaderBoardGroup.All);
    }

    public Task Hide()
    {
        backStackHandler.MenuHidden();
        loadingIndicator?.Dispose();
        gameObject.SetActive(false);

        return Task.CompletedTask;
    }

    public void SwitchToXP() => ChangeLeaderBoard(LeaderBoardSubject.XP, LeaderBoardGroup.All);

    public void SwitchToScore() => ChangeLeaderBoard(LeaderBoardSubject.Score, LeaderBoardGroup.All);

    void SetButtonState(Button button, bool active)
    {
        (button.targetGraphic as Image).sprite = active ? activeButton : inactiveButton;
        button.interactable = !active;
    }

    void ChangeLeaderBoard(LeaderBoardSubject subject, LeaderBoardGroup group)
    {
        TaskExtensions.RunIgnoreAsync(async () =>
        {
            SetButtonState(xpButton, subject == LeaderBoardSubject.XP);
            SetButtonState(rankButton, subject == LeaderBoardSubject.Score);

            SetButtonState(allButton, group == LeaderBoardGroup.All);
            SetButtonState(friendsButton, group == LeaderBoardGroup.Friends);
            SetButtonState(groupButton, group == LeaderBoardGroup.Clan);

            if (!entryCache.TryGetValue((subject, group), out var entries))
            {
                using (loadingIndicator = LoadingIndicator.Show(false))
                    entries = await ConnectionManager.Instance.EndPoint<SystemEndPoint>().GetLeaderBoard(subject, group);
                loadingIndicator = null;
                entryCache[(subject, group)] = entries;
            }

            if (isActiveAndEnabled)
                StartCoroutine(ShowLeaderBoard(entries));
        });
    }

    IEnumerator ShowLeaderBoard(IReadOnlyList<LeaderBoardEntryDTO> entries)
    {
        var ownEntryProxy = default(Transform);
        var ownScore = 0ul;
        var ownRank = 0ul;

        void SetupEntry(Transform tr, string name, ulong score, ulong rank, bool highlight, AvatarDTO avatar)
        {
            Translation.SetTextNoTranslate(tr.Find("Score").GetComponent<TextMeshProUGUI>(), score.ToString());
            Translation.SetTextNoTranslate(tr.Find("Name").GetComponent<TextMeshProUGUI>(), name);
            Translation.SetTextNoTranslate(tr.Find("Rank/Text").GetComponent<TextMeshProUGUI>(), rank.ToString());

            var highlightTransform = tr.Find("Highlight");
            if (highlightTransform)
                highlightTransform.gameObject.SetActive(highlight);

            var rankFrame = tr.Find("Rank").GetComponent<Image>();
            if (rank == 1)
                rankFrame.sprite = rank1st;
            else if (rank == 2)
                rankFrame.sprite = rank2nd;
            else if (rank == 3)
                rankFrame.sprite = rank3rd;
            else
                rankFrame.sprite = rankOthers;

            tr.Find("Avatar").GetComponent<AvatarDisplay>().SetAvatar(avatar);
        }

        entriesList.ClearContainer();

        var odd = true;
        var lastRank = 0ul;
        foreach (var entry in entries)
        {
            if (lastRank != 0 && lastRank != entry.Rank - 1)
                entriesList.AddListItem(discontinuityTemplate);
            else
                odd = !odd;

            lastRank = entry.Rank;

            if (entry.Info?.Name == null)
            {
                ownScore = entry.Score;
                ownRank = entry.Rank;
                ownEntryProxy = entriesList.AddListItem(ownEntryProxyTemplate);
            }
            else
            {
                SetupEntry(entriesList.AddListItem(entryTemplate), entry.Info.Name, entry.Score, entry.Rank, odd, entry.Info.Avatar);
            }
        }

        if (ownEntryProxy == null)
            ownScoreEntry.Hide();
        else
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var td = TransientData.Instance;
            SetupEntry(ownScoreEntry.transform, td.UserName, ownScore, ownRank, false, td.Avatar);
            ownScoreEntry.Show(ownEntryProxy);
        }
    }
}
