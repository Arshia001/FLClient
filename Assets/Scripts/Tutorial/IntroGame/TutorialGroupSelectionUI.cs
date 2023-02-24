using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialGroupSelectionUI : MonoBehaviour
{
    Button refreshGroupsButton;

    void Awake()
    {
        refreshGroupsButton = transform.Find("NewGroupsButton").GetComponent<Button>();

        Hide();
    }

    public void Show(GroupInfoDTO group1, GroupInfoDTO group2, GroupInfoDTO group3)
    {
        gameObject.SetActive(true);
        refreshGroupsButton.interactable = true;

        ShowGroup(group1, 1);
        ShowGroup(group2, 2);
        ShowGroup(group3, 3);

        Translation.SetTextNoShape(refreshGroupsButton.transform.Find("PriceText").GetComponent<TextMeshProUGUI>(),
            $"{MoneySprites.CoinStack} {PersianTextShaper.PersianTextShaper.ShapeText(TransientData.Instance.ConfigValues.PriceToRefreshGroups.ToString(), rightToLeftRenderDirection: true)}");
    }

    void ShowGroup(GroupInfoDTO group, int index)
    {
        var tr = transform.Find($"Groups/{index}");
        Translation.SetTextNoTranslate(tr.Find("Text").GetComponent<TextMeshProUGUI>(), group.Name);
    }

    public void Hide() => gameObject.SetActive(false);
}
