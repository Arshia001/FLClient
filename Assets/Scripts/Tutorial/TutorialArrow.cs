using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialArrow : MonoBehaviour
{
    RectTransform target;
    Direction direction;

    public void Show(RectTransform target, Direction direction)
    {
        gameObject.SetActive(true);

        this.target = target;
        this.direction = direction;

        UpdateTransform();
    }

    void Update() => UpdateTransform();

    void UpdateTransform()
    {
        // Direction enum is in order, each clockwise step is -45 degrees in Unity coordinates
        var rotation = -45.0f * (int)direction;
        transform.rotation = Quaternion.Euler(0, 0, rotation);

        var rect = target.GetWorldSpaceRect();

        var position = default(Vector2);

        switch (direction)
        {
            case Direction.BottomRight:
                position = new Vector2(rect.xMax, rect.yMin);
                break;
            case Direction.Bottom:
                position = new Vector2(rect.center.x, rect.yMin);
                break;
            case Direction.BottomLeft:
                position = new Vector2(rect.xMin, rect.yMin);
                break;
            case Direction.Left:
                position = new Vector2(rect.xMin, rect.center.y);
                break;
            case Direction.TopLeft:
                position = new Vector2(rect.xMin, rect.yMax);
                break;
            case Direction.Top:
                position = new Vector2(rect.center.x, rect.yMax);
                break;
            case Direction.TopRight:
                position = new Vector2(rect.xMax, rect.yMax);
                break;
            case Direction.Right:
                position = new Vector2(rect.xMax, rect.center.y);
                break;
        }

        transform.localPosition = transform.parent.InverseTransformPoint(position);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public enum Direction
    {
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        TopLeft,
        Top,
        TopRight,
        Right,
    }
}
