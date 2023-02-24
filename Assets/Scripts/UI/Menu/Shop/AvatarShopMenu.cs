using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Network;
using Network.Types;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class AvatarShopMenu : MonoBehaviour
{
    static readonly IEnumerable<AvatarPartType> allPartTypes = new[] { AvatarPartType.Eyes, AvatarPartType.Glasses, AvatarPartType.Hair, AvatarPartType.HeadShape, AvatarPartType.Mouth };

    [SerializeField] Sprite activeTab = default, inactiveTab = default;
    [SerializeField] Color inactiveTabColor = default;
    [SerializeField] Color insufficientLevelColor = default;

    Dictionary<AvatarPartType, Image> tabs;
    Dictionary<AvatarPartType, GameObject> partLists;
    Dictionary<(AvatarPartType, ushort?), PartDisplayInfo> parts;

    AvatarDisplay avatarDisplay;
    AvatarDTO avatar;

    public RectTransform AcceptButtonTransform => transform.Find("Buttons/Accept").transform as RectTransform;

    void Awake()
    {
        avatarDisplay = transform.Find("Preview/Avatar").GetComponent<AvatarDisplay>();

        tabs = new Dictionary<AvatarPartType, Image>();
        var tr = transform.Find("Tabs");
        for (var i = 0; i < tr.childCount; ++i)
        {
            if (!Enum.TryParse<AvatarPartType>(tr.GetChild(i).name, out var part))
                throw new Exception($"Unknown part type {tr.GetChild(i).name} in tab name");

            tabs[part] = tr.GetChild(i).GetComponent<Image>();
        }

        avatar = TransientData.Instance.Avatar;
        InitializeParts();
        ResetUI();
    }

    void ActivateTab(AvatarPartType type)
    {
        foreach (var t in tabs.Values)
        {
            t.sprite = inactiveTab;
            t.color = inactiveTabColor;
            t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 96);
        }
        foreach (var p in partLists.Values)
            p.SetActive(false);

        var img = tabs[type];
        img.sprite = activeTab;
        img.color = Color.white;
        img.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 97);
        partLists[type].SetActive(true);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Show();
        else
            Hide();
    }

    public async Task ShowExitConfirmationIfNeeded()
    {
        if (!gameObject.activeInHierarchy || avatar.IsEquivalentTo(TransientData.Instance.Avatar))
            return;

        if (await DialogBox.Instance.Show("می‌خوای آواتاری که انتخاب کردی رو ذخیره کنی؟", "آره", "نه") != DialogBox.Result.Yes)
            return;

        await ActivateSelectedAvatar();
    }

    private void Hide() => gameObject.SetActive(false);

    void Show()
    {
        gameObject.SetActive(true);

        if (avatar != null)
            avatarDisplay.SetAvatar(avatar);
    }

    void InitializeParts()
    {
        partLists = new Dictionary<AvatarPartType, GameObject>();
        parts = new Dictionary<(AvatarPartType, ushort?), PartDisplayInfo>();

        var td = TransientData.Instance;
        var apr = AvatarPartRepository.Instance;

        var parent = transform.Find("Parts");
        var template = parent.Find("Template").gameObject;

        parent.ClearContainer();

        foreach (var partType in allPartTypes)
        {
            var tr = parent.AddListItem(template);
            partLists[partType] = tr.gameObject;

            tr.name = partType.ToString();

            var partParent = tr.Find("Viewport/Content");
            var partTemplate = partParent.Find("Template").gameObject;

            var orderedIDs =
                apr.GetPartIDs(partType)
                .Select(id => td.AvatarParts[new AvatarPartDTO(partType, id)])
                .OrderBy(p => p.MinimumLevel)
                .ThenBy(p => p.Price)
                .ThenBy(p => p.ID)
                .Select(p => p.ID);

            partParent.ClearContainer();

            if (partType == AvatarPartType.Glasses || partType == AvatarPartType.Hair)
            {
                var removePartTr = partParent.AddListItem(partParent.Find("RemoveTemplate").gameObject);

                var button = removePartTr.GetComponent<Button>();
                var capturedType = partType;
                button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectPart(capturedType, null)));

                var displayInfo = new PartDisplayInfo(partType, null, removePartTr.Find("Footer/Selected").GetComponent<Image>(), removePartTr.Find("Footer/Price").GetComponent<TextMeshProUGUI>());
                parts[(partType, null)] = displayInfo;

                UpdatePartDisplay(displayInfo);
            }

            foreach (var id in orderedIDs)
            {
                var partTr = partParent.AddListItem(partTemplate);

                var button = partTr.GetComponent<Button>();
                var capturedType = partType;
                var capturedID = id;
                button.onClick.AddListener(new UnityEngine.Events.UnityAction(() => SelectPart(capturedType, capturedID)));

                AvatarDisplay.CreatePreview(partTr.Find("Graphics/Graphic").GetComponent<Image>(), partTr.Find("Graphics/HairDeco").GetComponent<Image>(), partType, id);

                var displayInfo = new PartDisplayInfo(partType, id, partTr.Find("Footer/Selected").GetComponent<Image>(), partTr.Find("Footer/Price").GetComponent<TextMeshProUGUI>());
                parts[(partType, id)] = displayInfo;

                UpdatePartDisplay(displayInfo);
            }
        }
    }

    void UpdatePartDisplay(PartDisplayInfo part)
    {
        var td = TransientData.Instance;
        var dto = part.ID.HasValue ? new AvatarPartDTO(part.PartType, part.ID.Value) : null;
        var partConfig = dto == null ? null : td.AvatarParts[dto];

        if (dto == null || td.OwnedAvatarParts.Contains(dto))
            Translation.SetTextNoShape(part.Price, "");
        else if (partConfig.MinimumLevel > td.Level)
        {
            Translation.SetTextNoTranslate(part.Price, $"سطح {partConfig.MinimumLevel}");
            part.Price.color = insufficientLevelColor;
        }
        else
        {
            part.Price.color = Color.black;

            var price = partConfig.Price;
            if (price == 0)
                Translation.SetTextNoShape(part.Price, "");
            else
                Translation.SetTextNoShape(part.Price, GoldDisplay.GetText(price, GoldDisplay.SpritePosition.Before));
        }

        if (dto == null)
            part.Selected.gameObject.SetActive(avatar.GetPart(part.PartType) == null);
        else
            part.Selected.gameObject.SetActive(avatar.IsPartActive(dto));
    }

    void SelectPart(AvatarPartType partType, ushort? id)
    {
        var existing = avatar.GetPart(partType);
        if (id.HasValue)
            avatar = avatar.ReplacePart(new AvatarPartDTO(partType, id.Value));
        else
            avatar = avatar.RemovePart(partType);

        avatarDisplay.SetAvatar(avatar);

        UpdatePartDisplay(parts[(partType, id)]);
        if (existing.HasValue)
            UpdatePartDisplay(parts[(partType, existing.Value)]);
        else if (parts.TryGetValue((partType, null), out var displayInfo))
            UpdatePartDisplay(displayInfo);
    }

    async Task ActivateSelectedAvatar()
    {
        var td = TransientData.Instance;
        var notOwned = new List<AvatarPartDTO>();
        var maxInsufficientLevel = default(ushort?);
        var totalPrice = 0u;

        if (avatar.IsEquivalentTo(td.Avatar))
        {
            InformationToast.Instance.Enqueue("هنوز آواتارتو عوض نکردی که!");
            return;
        }

        foreach (var part in avatar.Parts)
        {
            var partConfig = td.AvatarParts[part];
            var havePart = td.OwnedAvatarParts.Contains(part);
            if (!havePart && partConfig.Price > 0)
            {
                notOwned.Add(part);
                totalPrice += td.AvatarParts[part].Price;
            }
            if (!havePart && partConfig.MinimumLevel > td.Level)
                maxInsufficientLevel = maxInsufficientLevel.HasValue ? Math.Max(maxInsufficientLevel.Value, partConfig.MinimumLevel) : partConfig.MinimumLevel;
        }

        if (maxInsufficientLevel.HasValue)
        {
            await DialogBox.Instance.Show($"برای خریدن این آواتار باید به سطح {maxInsufficientLevel.Value} برسی.", "باشه");
            return;
        }

        var ep = ConnectionManager.Instance.EndPoint<SystemEndPoint>();

        if (notOwned.Any())
        {
            if (td.Gold.Value < totalPrice)
            {
                if (await DialogBox.Instance.Show($"برای خریدن این آواتار {totalPrice - td.Gold.Value} سکه کم داری.‌ می‌خوای بریم فروشگاه؟", "بریم", "بی‌خیال") == DialogBox.Result.Yes)
                    MenuManager.Instance.Menu<ShopMenu>().ShowCoinShop();
                return;
            }

            if (await DialogBox.Instance.Show($"برای خریدن این آواتار باید {totalPrice} سکه پرداخت کنی. مطمئنی می‌خوای بخریش؟", "آره", "بی‌خیال") == DialogBox.Result.No)
                return;

            try
            {
                using (LoadingIndicator.Show(true))
                {
                    var (success, gold) = await ep.BuyAvatarParts(notOwned);
                    if (success)
                    {
                        td.Gold.Value = gold;
                        foreach (var part in notOwned)
                        {
                            td.OwnedAvatarParts.Add(part);
                            UpdatePartDisplay(parts[(part.PartType, part.ID)]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                InformationToast.Instance.Enqueue("خرید آواتار با خطا مواجه شد. بعدا دوباره تلاش کن.");
                Debug.LogException(ex);
                return;
            }
        }

        try
        {
            foreach (var part in parts)
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent($"AvatarActive:{part.Key}:{part.Value?.ToString() ?? "REMOVED"}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        try
        {
            using (LoadingIndicator.Show(true))
            {
                await ep.ActivateAvatar(avatar);
                td.Avatar.Value = avatar;
                InformationToast.Instance.Enqueue("آواتار جدیدت فعال شد!");
            }
        }
        catch (Exception ex)
        {
            InformationToast.Instance.Enqueue("فعال کردن آواتار با خطا مواجه شد. بعدا دوباره تلاش کن.");
            Debug.LogException(ex);
        }
    }

    public void SaveSelection() => TaskExtensions.RunIgnoreAsync(ActivateSelectedAvatar);

    public void ResetSelection()
    {
        var avatar = TransientData.Instance.Avatar.Value;
        foreach (var type in allPartTypes)
            SelectPart(type, avatar.GetPart(type));
        avatarDisplay.SetAvatar(avatar);
    }

    public void ResetUI()
    {
        // Not initialized yet, will be reset after initialization anyway
        if (avatarDisplay == null)
            return;

        ShowHeadShape();
        ResetSelection();
    }

    public void ShowHeadShape() => ActivateTab(AvatarPartType.HeadShape);

    public void ShowEyes() => ActivateTab(AvatarPartType.Eyes);

    public void ShowMouth() => ActivateTab(AvatarPartType.Mouth);

    public void ShowHair() => ActivateTab(AvatarPartType.Hair);

    public void ShowGlasses() => ActivateTab(AvatarPartType.Glasses);

    class PartDisplayInfo
    {
        public PartDisplayInfo(AvatarPartType partType, ushort? id, Image selected, TextMeshProUGUI price)
        {
            PartType = partType;
            ID = id;
            Selected = selected;
            Price = price;
        }

        public AvatarPartType PartType { get; }
        public ushort? ID { get; }
        public Image Selected { get; }
        public TextMeshProUGUI Price { get; }
    }
}
