using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformExtensions
{
    public static Rect GetWorldSpaceRect(this RectTransform target)
    {
        var corners = new Vector3[4];
        target.GetWorldCorners(corners);

        var xMin = float.MaxValue;
        var xMax = float.MinValue;
        var yMin = float.MaxValue;
        var yMax = float.MinValue;

        foreach (var c in corners)
        {
            if (c.x < xMin)
                xMin = c.x;
            if (c.x > xMax)
                xMax = c.x;
            if (c.y < yMin)
                yMin = c.y;
            if (c.y > yMax)
                yMax = c.y;
        }

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }
}
