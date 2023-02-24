using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMessageBox : MonoBehaviour
{
    [SerializeField] Sprite[] characterImages = default;

    TextMeshProUGUI text;
    GameObject okButton;
    Image character;

    void Awake()
    {
        text = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        okButton = transform.Find("OKButton").gameObject;
        character = transform.Find("Character").GetComponent<Image>();
    }

    public void Show(string message, bool showOKButton)
    {
        gameObject.SetActive(true);
        okButton.SetActive(showOKButton);
        Translation.SetText(text, message);
        if (characterImages.Length > 0)
            character.sprite = characterImages[Random.Range(0, characterImages.Length)];
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OKTapped() => TutorialManager.Instance.OKButtonTapped();
}
