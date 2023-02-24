using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DotPatternAspect : MonoBehaviour
{
    RectTransform rect;
    Graphic graphic;
    ChangeTrackerValue<Vector2> size = new ChangeTrackerValue<Vector2>();

    void Start()
    {
        rect = GetComponent<RectTransform>();
        graphic = GetComponent<Graphic>();

        size.ValueChanged += s => graphic.material.SetFloat("_Aspect", s.x / s.y);
        size.Update(rect.rect.size);
    }

    void Update() => size.Update(rect.rect.size);
}
