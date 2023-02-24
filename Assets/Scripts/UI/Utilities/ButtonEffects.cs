using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonEffects : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] bool backButton = default;
    [SerializeField] bool enableShadow = true;
    [SerializeField] GameObject imageObject = default;
    [SerializeField] float shadowOffset = 4.0f;
    [SerializeField] float shadowGrow = 2.0f;

    Button button;
    RectTransform shadowEffect;

    void Awake() => button = transform.GetComponentInParent<Button>();

    void Start()
    {
        if (enableShadow)
            CreateShadowEffect(GetImageGO());
    }

    GameObject GetImageGO()
    {
        if (imageObject)
            return imageObject;

        var button = GetComponent<Button>();
        if (button == null)
            return null;

        if (button.targetGraphic)
            return imageObject = button.targetGraphic.gameObject;

        Debug.LogWarning($"Button effect on {gameObject.name} has no target graphic to attach shadow to, attaching to own GO instead");
        return imageObject = gameObject;
    }

    void CreateShadowEffect(GameObject imageObject)
    {
        if (imageObject == null)
            return;

        var go = Instantiate(imageObject);
        go.name = imageObject.name + "_ShadowEffect";
        go.SetActive(imageObject.activeSelf);

        //?? isn't it easier to just clone the image and put it on a new GO?
        foreach (var component in go.GetComponents<Component>())
            if (component.GetType() != typeof(Image) &&
                component.GetType() != typeof(RectTransform) &&
                component.GetType() != typeof(CanvasRenderer))
                Destroy(component);

        for (var i = 0; i < go.transform.childCount; ++i)
            Destroy(go.transform.GetChild(i).gameObject);

        var tr = go.transform;
        tr.SetParent(imageObject.transform.parent, false);
        tr.SetSiblingIndex(imageObject.transform.GetSiblingIndex());

        var lg = go.AddComponent<LayoutElement>();
        lg.ignoreLayout = true;

        var image = tr.GetComponent<Image>();
        image.color = new Color(0, 0, 0, 0.4f);
        image.material = null;

        shadowEffect = tr as RectTransform;

        UpdateShadowEffectTransform();
    }

    void UpdateShadowEffectTransform()
    {
        if (shadowEffect)
        {
            var rect = imageObject.transform as RectTransform;
            shadowEffect.anchorMin = rect.anchorMin;
            shadowEffect.anchorMax = rect.anchorMax;
            shadowEffect.anchoredPosition = rect.anchoredPosition + new Vector2(shadowOffset, -shadowOffset);
            shadowEffect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rect.rect.width + shadowGrow);
            shadowEffect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.height + shadowGrow);

            shadowEffect.gameObject.SetActive(button.isActiveAndEnabled && button.IsInteractable());
        }
    }

    void LateUpdate() => UpdateShadowEffectTransform();

    void OnEnable() => shadowEffect?.gameObject.SetActive(true);

    void OnDisable() => shadowEffect?.gameObject.SetActive(false);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (button == null || button.IsInteractable())
            SoundEffectManager.Play(backButton ? SoundEffect.BackButtonPress : SoundEffect.ButtonPress);
    }
}
