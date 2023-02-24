using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class UIMarginSetter : MonoBehaviour
{
    [SerializeField] public bool horizontalEnabled;
    [SerializeField] public int horizontalMargin;

    RectTransform me, canvas;


    [ExecuteInEditMode]
    void Start()
    {
        me = transform as RectTransform;
        canvas = transform.root.GetComponent<Canvas>().transform as RectTransform;
    }

    [ExecuteInEditMode]
    void Update()
    {
        if (horizontalEnabled)
        {
            var size = me.sizeDelta;
            size.x = canvas.sizeDelta.x - horizontalMargin * 2;
            me.sizeDelta = size;
        }
    }
}
