using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderBoardOwnScoreEntry : MonoBehaviour
{
    Transform proxy;
    RectTransform container;
    RectTransform ownTransform;

    void Awake()
    {
        container = transform.parent as RectTransform;
        ownTransform = transform as RectTransform;

        UpdatePosition();
    }

    public void Hide() => gameObject.SetActive(false);

    public void Show(Transform proxy)
    {
        gameObject.SetActive(true);
        this.proxy = proxy;
        UpdatePosition();
    }

    void LateUpdate() => UpdatePosition();

    void UpdatePosition()
    {
        if (!proxy)
        {
            Hide();
            return;
        }

        var center = container.rect.center;
        var top = new Vector3(center.x, center.y + container.rect.height / 2 - ownTransform.rect.height / 2);
        var bottom = new Vector3(center.x, center.y - container.rect.height / 2 + ownTransform.rect.height / 2);
        var maxPosY = container.parent.localToWorldMatrix.MultiplyPoint(top).y;
        var minPosY = container.parent.localToWorldMatrix.MultiplyPoint(bottom).y;

        var posY = proxy.position.y;
        posY = Mathf.Clamp(posY, minPosY, maxPosY);

        var pos = transform.position;
        pos.y = posY;
        transform.position = pos;
    }
}
