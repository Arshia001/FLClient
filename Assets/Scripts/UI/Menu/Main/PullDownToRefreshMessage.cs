using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PullDownToRefreshMessage : MonoBehaviour
{
    Transform pullDownMessage;
    Graphic pullDownMessageGfx;
    Transform releaseMessage;
    Graphic releaseMessageGfx;
    RectTransform myRect;
    PullDownToRefresh pdtr;

    void Start()
    {
        pdtr = transform.parent.Find("List").GetComponent<PullDownToRefresh>();
        pdtr.onPullDown.AddListener(new UnityEngine.Events.UnityAction(OnPullDown));
        pdtr.onDistanceChanged.AddListener(new UnityEngine.Events.UnityAction<float>(OnDistanceChanged));

        myRect = transform as RectTransform;
        pullDownMessage = transform.GetChild(0);
        pullDownMessageGfx = pullDownMessage.GetComponent<Graphic>();
        releaseMessage = transform.GetChild(1);
        releaseMessageGfx = releaseMessage.GetComponent<Graphic>();
    }

    void OnDistanceChanged(float normalizedDistance)
    {
        // Ignore some of it to keep the message from popping into view when the scroll rect reaches the top edge after a scroll and goes past elastically
        normalizedDistance = Mathf.Clamp01(normalizedDistance * 1.2f - 0.2f);

        var rect = myRect.rect;
        var pos = new Vector2(0, Mathf.Lerp(rect.yMax, rect.yMin, normalizedDistance));
        pullDownMessage.localPosition = pos;
        releaseMessage.localPosition = pos;

        var color = new Color(0, 0, 0, Mathf.Clamp01(normalizedDistance + 0.25f));
        pullDownMessageGfx.color = color;
        releaseMessageGfx.color = color;

        var dragging = pdtr.Dragging;
        pullDownMessage.gameObject.SetActive(normalizedDistance < 1 && dragging);
        releaseMessage.gameObject.SetActive(normalizedDistance >= 1 && dragging);
    }

    void OnPullDown()
    {
        MenuManager.Instance.Menu<MainMenu>().RefreshGames();
    }
}
