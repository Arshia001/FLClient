using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    public static void ClearContainer(this Transform transform)
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            var child = transform.GetChild(i);
            if (child.gameObject.activeSelf)
                Object.Destroy(child.gameObject);
        }
    }

    public static Transform AddListItem(this Transform transform, GameObject template)
    {
        var go = Object.Instantiate(template);
        go.SetActive(true);

        var tr = go.transform;
        tr.SetParent(transform, false);

        return tr;
    }
}
