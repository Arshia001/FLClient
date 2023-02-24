using System.Collections;
using System.Collections.Generic;
using Network;
using TMPro;
using UnityEngine;

public class LevelUpNotification : MonoBehaviour
{
    TextMeshProUGUI text;
    uint level;

    public event System.Action Hidden;

    void Awake()
    {
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        gameObject.SetActive(false);
    }

    public void Show(uint level)
    {
        this.level = level;
        Translation.SetTextNoTranslate(text, $"تبریک، به سطح {level} رسیدی!");
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        ConnectionManager.Instance.EndPoint<SystemEndPoint>().SetNotifiedLevel(level);
        TransientData.Instance.NotifiedLevel.Value = level;
        gameObject.SetActive(false);
        Hidden?.Invoke();
    }
}
