using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class PullDownToRefresh : MonoBehaviour
{
    [Serializable]
    public class DistanceChangedEvent : UnityEvent<float> { }

    [SerializeField] int pullDownDistance = 50;

    public UnityEvent onPullDown = null;
    public DistanceChangedEvent onDistanceChanged = null;

    ScrollRect scrollRect;
    float initPosition;
    float prevDistance;
    bool prevDragging;
    bool eventRaised;

    FieldInfo draggingField;

    float GetPosition() => scrollRect.content.position.y / scrollRect.content.lossyScale.y;

    bool IsDragging() => (bool)draggingField.GetValue(scrollRect);

    public bool Dragging => prevDragging;

    // Start is called before the first frame update
    void Start()
    {
        scrollRect = GetComponent<ScrollRect>();
        initPosition = GetPosition();

        draggingField = typeof(ScrollRect).GetField("m_Dragging", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    // Update is called once per frame
    void Update()
    {
        var distance = initPosition - GetPosition();
        if (distance != prevDistance)
            onDistanceChanged.Invoke(Mathf.Clamp01(distance / pullDownDistance));

        if (IsDragging())
            prevDragging = true;
        else
        {
            if (prevDragging && distance >= pullDownDistance && !eventRaised)
            {
                eventRaised = true;
                onPullDown.Invoke();
            }

            if (distance < pullDownDistance)
                eventRaised = false;

            prevDragging = false;
        }

        prevDistance = distance;
    }
}
