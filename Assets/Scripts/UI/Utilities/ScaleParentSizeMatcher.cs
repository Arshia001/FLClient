using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class ScaleParentSizeMatcher : MonoBehaviour
{
    Vector2 initParentSize;
    float initScale;

    void Start()
    {
        initParentSize = (transform.parent as RectTransform).rect.size;
        initScale = transform.localScale.x;
    }

    void Update()
    {
        var parentSize = (transform.parent as RectTransform).rect.size;
        var scaleFactor = Mathf.Min(parentSize.x / initParentSize.x, parentSize.y / initParentSize.y);
        transform.localScale = scaleFactor * initScale * Vector3.one;
    }
}
