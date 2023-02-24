using System.Collections;
using System.Collections.Generic;
using TapsellPlusSDK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BannerAd : MonoBehaviour
{
    [SerializeField] AdRepository.AdZone zone = default;

    GameObject frame;
    Image icon;
    TextMeshProUGUI title;

    TapsellPlusNativeBannerAd ad;
    Texture2D iconImage;

    void Awake()
    {
        frame = transform.Find("Frame").gameObject;
        icon = transform.Find("Frame/Icon").GetComponent<Image>();
        title = transform.Find("Frame/Title").GetComponent<TextMeshProUGUI>();

        // No need for this object to possibly be visible for one whole frame if Update doesn't run for the current frame
        frame.SetActive(false);
    }

    void Update()
    {
        var available = AdRepository.Instance.IsAdAvailable(zone);

        frame.SetActive(available);

        if (available)
        {
            var newAd = AdRepository.Instance.GetNativeBanner(zone);

            if (newAd != null)
            {
                if (newAd != ad)
                {
                    ad = newAd;

                    SetIconImage(ad.iconImage);

                    Translation.SetTextNoTranslate(title, ad.title);
                }
                else
                {
                    var newIconImage = newAd.iconImage;
                    if (newIconImage != iconImage)
                        SetIconImage(newIconImage);
                }
            }
            else
                frame.SetActive(false); // Can't ever be too sure
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

    public void Clicked() => AdRepository.Instance.NativeBannerClicked(zone);
}
