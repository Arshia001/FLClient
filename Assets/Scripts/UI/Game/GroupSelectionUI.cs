using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GroupSelectionUI : SingletonBehaviour<GroupSelectionUI>, IGameMenu
{
    Button refreshGroupsButton;

    MenuBackStackHandler backStackHandler = new MenuBackStackHandler(() => false);

    public RectTransform RefreshGroupsButton => refreshGroupsButton.transform as RectTransform;

    int remainingRefreshes;

    protected override void Awake()
    {
        base.Awake();

        refreshGroupsButton = transform.Find("NewGroupsButton").GetComponent<Button>();
    }

    public void Show(GroupInfoDTO group1, GroupInfoDTO group2, GroupInfoDTO group3, int numRemainingRefreshes)
    {
        backStackHandler.MenuShown();

        gameObject.SetActive(true);
        refreshGroupsButton.interactable = numRemainingRefreshes > 0;

        ShowGroup(group1, 1);
        ShowGroup(group2, 2);
        ShowGroup(group3, 3);
        remainingRefreshes = numRemainingRefreshes;

        var text = refreshGroupsButton.transform.Find("PriceText").GetComponent<TextMeshProUGUI>();
        text.isRightToLeftText = true;
        Translation.SetTextNoShape(text, $"{MoneySprites.CoinStack} {PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.ConfigValues.PriceToRefreshGroups.ToString(), rightToLeftRenderDirection: true)}");

        TutorialManager.Instance.GroupChoicesShown();
    }

    void ShowGroup(GroupInfoDTO group, int index)
    {
        var tr = transform.Find($"Groups/{index}");
        Translation.SetTextNoTranslate(tr.Find("Text").GetComponent<TextMeshProUGUI>(), group.Name);
        var button = tr.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => ChooseGroup(group.ID)));
    }

    public void RefreshGroups()
    {
        if (TransientData.Instance.Gold >= TransientData.Instance.ConfigValues.PriceToRefreshGroups)
        {
            refreshGroupsButton.interactable = false;
            GameManager.Instance.RefreshGroupChoices(remainingRefreshes);
        }
        else
        {
            InformationToast.Instance.Enqueue("پول کافی نداری!");
        }
    }

    public void ChooseGroup(int id) => GameManager.Instance.ChooseGroup(id);

    public Task Hide()
    {
        backStackHandler.MenuHidden();
        gameObject.SetActive(false);

        return Task.CompletedTask;
    }

    public void Show() => throw new NotSupportedException();
}
