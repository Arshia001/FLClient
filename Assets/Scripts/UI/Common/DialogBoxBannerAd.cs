using System.Collections;
using System.Collections.Generic;
using TapsellPlusSDK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogBoxBannerAd : MonoBehaviour
{
    GameObject root;
    Image icon;
    Image banner;
    TextMeshProUGUI title;
    TextMeshProUGUI subtitle;

    AdRepository.AdZone? zone;
    TapsellPlusNativeBannerAd ad;
    Texture2D iconImage;
    Texture2D bannerImage;

    public AdRepository.AdZone? Zone
    {
        get => zone;

        set
        {
            zone = value;
            ad = null;
            if (root)
                root.SetActive(false);
        }
    }

    void Awake()
    {
        root = transform.Find("Frame/Ad").gameObject;
        banner = transform.Find("Frame/Ad/Image").GetComponent<Image>();
        icon = transform.Find("Frame/Ad/Header/Icon").GetComponent<Image>();
        title = transform.Find("Frame/Ad/Header/Title").GetComponent<TextMeshProUGUI>();
        subtitle = transform.Find("Frame/Ad/Header/Subtitle").GetComponent<TextMeshProUGUI>();

        // No need for this object to possibly be visible for one whole frame if Update doesn't run for the current frame
        root.SetActive(false);
    }

    void Update()
    {
        var available = zone.HasValue && AdRepository.Instance.IsAdAvailable(zone.Value);

        root.SetActive(available);

        if (available)
        {
            var newAd = AdRepository.Instance.GetNativeBanner(zone.Value);

            // Can't be too sure
            if (newAd != null)
            {
                if (newAd != ad)
                {
                    ad = newAd;

                    SetBannerImage(GetBannerImage(ad));

                    SetIconImage(iconImage);

                    Translation.SetTextNoTranslate(title, ad.title);
                    Translation.SetTextNoTranslate(subtitle, ad.description);
                }
                else
                {
                    var newBannerImage = GetBannerImage(newAd);
                    if (newBannerImage != bannerImage)
                        SetBannerImage(newBannerImage);

                    var newIconImage = newAd.iconImage;
                    if (newIconImage != iconImage)
                        SetIconImage(newIconImage);
                }
            }
            else
                root.SetActive(false); // Can't ever be too sure
        }
    }

    private void SetIconImage(Texture2D iconImage)
    {
        this.iconImage = iconImage;

        if (iconImage != null)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = Sprite.Create(iconImage,
                new Rect(0, 0, iconImage.width, iconImage.height),
                new Vector2(iconImage.width / 2, iconImage.height / 2)
                );
        }
        else
            icon.gameObject.SetActive(false);
    }

    private Texture2D GetBannerImage(TapsellPlusNativeBannerAd ad)
    {
        return ad.landscapeBannerImage ?? ad.portraitBannerImage;
    }

    private void SetBannerImage(Texture2D bannerImage)
    {
        this.bannerImage = bannerImage;

        if (bannerImage == null)
            banner.gameObject.SetActive(false);
        else
        {
            banner.gameObject.SetActive(true);
            banner.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Min(1.0f, bannerImage.height / (float)bannerImage.width) * banner.rectTransform.rect.width
                );
            banner.sprite = Sprite.Create(bannerImage,
                new Rect(0, 0, bannerImage.width, bannerImage.height),
                new Vector2(bannerImage.width / 2, bannerImage.height / 2)
                );
        }
    }

    public void Clicked() => AdRepository.Instance.NativeBannerClicked(zone.Value);
}
