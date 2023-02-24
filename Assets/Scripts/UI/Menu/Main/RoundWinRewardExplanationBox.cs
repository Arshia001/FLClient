using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoundWinRewardExplanationBox : MonoBehaviour
{
    [SerializeField] float duration = 2.0f;

    float deactivationTime;
    TextMeshProUGUI text;

    void Awake() => text = transform.Find("Text").GetComponent<TextMeshProUGUI>();

    void Start() => gameObject.SetActive(false);

    void Update()
    {
        if (Time.time > deactivationTime)
            gameObject.SetActive(false);
    }

    public void Show(string explanation)
    {
        if (gameObject.activeSelf)
            return;

        deactivationTime = Time.time + duration;
        Translation.SetTextNoTranslate(text, explanation);
        gameObject.SetActive(true);
    }
}
