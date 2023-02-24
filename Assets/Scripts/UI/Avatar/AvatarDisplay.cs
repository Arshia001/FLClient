using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplay : MonoBehaviour
{
    Dictionary<string, Head> heads;
    AvatarDTO initialAvatar;

    void Awake()
    {
        heads = new Dictionary<string, Head>();

        var scaler = transform.Find("Scaler");
        for (int i = 0; i < scaler.childCount; ++i)
        {
            var tr = scaler.GetChild(i);
            heads.Add(tr.name, new Head
            {
                gameObject = tr.gameObject,
                headShape = tr.Find("HeadShape").GetComponent<Image>(),
                hair = tr.Find("Hair").GetComponent<Image>(),
                hairBack = tr.Find("HairBack").GetComponent<Image>(),
                hairDeco = tr.Find("HairDeco").GetComponent<Image>(),
                hairBackDeco = tr.Find("HairBackDeco").GetComponent<Image>(),
                mouth = tr.Find("Mouth").GetComponent<Image>(),
                eyes = tr.Find("Eyes").GetComponent<Image>(),
                glasses = tr.Find("Glasses").GetComponent<Image>()
            });
        }

        if (initialAvatar != null)
        {
            SetAvatar(initialAvatar);
            initialAvatar = null;
        }
    }

    public void SetAvatar(AvatarDTO avatar)
    {
        if (heads == null)
        {
            initialAvatar = avatar;
            return;
        }

        foreach (var h in heads.Values)
            h.gameObject.SetActive(false);

        if (avatar == null)
            return;

        var parts = avatar.Parts.ToDictionary(part => part.PartType);

        var apr = AvatarPartRepository.Instance;

        var beardColor = parts.TryGetValue(AvatarPartType.Mouth, out var p) && apr.Mouths.TryGetValue(p.ID, out var m) ? m.color.Color : Color.white;
        var hairColor = parts.TryGetValue(AvatarPartType.Hair, out p) && apr.Hairs.TryGetValue(p.ID, out var hr) ? hr.color.Color : Color.white;
        var skinColor = parts.TryGetValue(AvatarPartType.HeadShape, out p) && apr.HeadShapes.TryGetValue(p.ID, out var hs) ? hs.color.Color : Color.white;

        Color GetColor(AvatarColorMap color)
        {
            switch (color)
            {
                case AvatarColorMap.Beard:
                    return beardColor;

                case AvatarColorMap.Hair:
                    return hairColor;

                case AvatarColorMap.Skin:
                    return skinColor;

                case AvatarColorMap.None:
                default:
                    return Color.white;
            }
        }

        if (!parts.TryGetValue(AvatarPartType.HeadShape, out p) || ! apr.HeadShapes.ContainsKey(p.ID))
        {
            Debug.LogWarning("No head shape, using head 1 as default");
            p = new AvatarPartDTO(AvatarPartType.HeadShape, 1);
        }

        var headDef = apr.HeadShapes[p.ID];
        var head = heads[headDef.graphic.Name];
        head.gameObject.SetActive(true);
        head.headShape.color = GetColor(headDef.graphic.ColorMap);

        if (parts.TryGetValue(AvatarPartType.Eyes, out p) && apr.Eyes.ContainsKey(p.ID))
        {
            head.eyes.gameObject.SetActive(true);
            var partDef = apr.Eyes[p.ID];
            head.eyes.sprite = partDef.Sprite;
            head.eyes.color = GetColor(partDef.ColorMap);
        }
        else
            head.eyes.gameObject.SetActive(false);

        if (parts.TryGetValue(AvatarPartType.Glasses, out p) && apr.Glasses.ContainsKey(p.ID))
        {
            head.glasses.gameObject.SetActive(true);
            var partDef = apr.Glasses[p.ID];
            head.glasses.sprite = partDef.Sprite;
            head.glasses.color = GetColor(partDef.ColorMap);
        }
        else
            head.glasses.gameObject.SetActive(false);

        if (parts.TryGetValue(AvatarPartType.Mouth, out p) && apr.Mouths.ContainsKey(p.ID))
        {
            head.mouth.gameObject.SetActive(true);
            var partDef = apr.Mouths[p.ID];
            head.mouth.sprite = partDef.graphic.Sprite;
            head.mouth.color = GetColor(partDef.graphic.ColorMap);
        }
        else
            head.mouth.gameObject.SetActive(false);

        if (parts.TryGetValue(AvatarPartType.Hair, out p) && apr.Hairs.ContainsKey(p.ID))
        {
            head.hair.gameObject.SetActive(true);
            var partDef = apr.Hairs[p.ID];
            head.hair.sprite = partDef.graphic.Sprite;
            head.hair.color = GetColor(partDef.graphic.ColorMap);

            if (partDef.graphic.BackSprite != null)
            {
                head.hairBack.gameObject.SetActive(true);
                head.hairBack.sprite = partDef.graphic.BackSprite;
                head.hairBack.color = GetColor(partDef.graphic.ColorMap);
            }
            else
                head.hairBack.gameObject.SetActive(false);

            if (partDef.graphic.DecoSprite != null)
            {
                head.hairDeco.gameObject.SetActive(true);
                head.hairDeco.sprite = partDef.graphic.DecoSprite;
            }
            else
                head.hairDeco.gameObject.SetActive(false);

            if (partDef.graphic.BackDecoSprite != null)
            {
                head.hairBackDeco.gameObject.SetActive(true);
                head.hairBackDeco.sprite = partDef.graphic.BackDecoSprite;
            }
            else
                head.hairBackDeco.gameObject.SetActive(false);
        }
        else
        {
            head.hair.gameObject.SetActive(false);
            head.hairBack.gameObject.SetActive(false);
            head.hairDeco.gameObject.SetActive(false);
            head.hairBackDeco.gameObject.SetActive(false);
        }

    }

    public static void CreatePreview(Image target, Image hairDeco, AvatarPartType partType, ushort id)
    {
        var apr = AvatarPartRepository.Instance;

        hairDeco.gameObject.SetActive(false);

        switch (partType)
        {
            case AvatarPartType.HeadShape:
                {
                    var part = apr.HeadShapes[id];
                    target.sprite = part.graphic.Sprite;
                    target.color = part.color.Color;
                    break;
                }

            case AvatarPartType.Eyes:
                {
                    var part = apr.Eyes[id];
                    target.sprite = part.Sprite;
                    target.color = Color.white;
                    break;
                }

            case AvatarPartType.Mouth:
                {
                    var part = apr.Mouths[id];
                    target.sprite = part.graphic.Sprite;
                    target.color = part.color.Color;
                    break;
                }

            case AvatarPartType.Hair:
                {
                    var part = apr.Hairs[id];
                    target.sprite = part.graphic.Sprite;
                    target.color = part.color.Color;
                    if (part.graphic.DecoSprite != null)
                    {
                        hairDeco.gameObject.SetActive(true);
                        hairDeco.sprite = part.graphic.DecoSprite;
                        hairDeco.color = Color.white;
                    }
                    break;
                }

            case AvatarPartType.Glasses:
                {
                    var part = apr.Glasses[id];
                    target.sprite = part.Sprite;
                    target.color = Color.white;
                    break;
                }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    class Head
    {
        public GameObject gameObject;
        public Image headShape, hair, hairBack, hairDeco, hairBackDeco, mouth, eyes, glasses;
    }
}
