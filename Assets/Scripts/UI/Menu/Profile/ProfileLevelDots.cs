using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ProfileLevelDots : MonoBehaviour
{
    [SerializeField] float angleInterval = -10;
    [SerializeField] float startAngle = 13;
    [SerializeField] float maxDots = 20;

    float fill;

    GameObject template;

    public float Fill 
    {
        get => fill;
        set
        {
            fill = value;
            RefreshDots();
        }
    }

    void Awake()
    {
        template = transform.Find("Template").gameObject;
        RefreshDots();
    }

    void RefreshDots()
    {
        if (!template)
            return;

        var count = Mathf.RoundToInt(maxDots * fill);

        var parent = transform;
        parent.ClearContainer();
        for (int i = 0; i < count; ++i)
        {
            var dot = parent.AddListItem(template);
            dot.name = i.ToString();
            dot.rotation = Quaternion.Euler(0, 0, startAngle + i * angleInterval);
        }
    }
}
