using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DialogBox : SingletonBehaviour<DialogBox>
{
    public enum Result
    {
        Yes,
        No,
        Cancel
    }

    TaskCompletionSource<Result> resultTcs;

    GameObject noButton, cancelButton;
    TextMeshProUGUI text, yesButtonText, noButtonText;

    DialogBoxBannerAd bannerAd;

    int backToken;

    protected override void Awake()
    {
        base.Awake();

        text = transform.Find("Frame/Text").GetComponent<TextMeshProUGUI>();
        yesButtonText = transform.Find("Frame/Buttons/Yes/Text").GetComponent<TextMeshProUGUI>();
        noButton = transform.Find("Frame/Buttons/No").gameObject;
        noButtonText = noButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        cancelButton = transform.Find("Frame/CancelButton").gameObject;

        bannerAd = GetComponent<DialogBoxBannerAd>();
    }

    void Start()
    {
        if (resultTcs == null)
            gameObject.SetActive(false);
    }

    Task<Result> Show(string text, string yesButtonText, string noButtonText, bool noButtonVisible, bool cancelButtonVisible, AdRepository.AdZone? adZone)
    {
        if (resultTcs != null)
            throw new Exception("Dialog already being shown");

        resultTcs = new TaskCompletionSource<Result>();

        Translation.SetTextNoTranslate(this.text, text);
        Translation.SetTextNoTranslate(this.yesButtonText, yesButtonText);
        noButton.SetActive(noButtonVisible);
        Translation.SetTextNoTranslate(this.noButtonText, noButtonText);
        cancelButton.SetActive(cancelButtonVisible);

        bannerAd.Zone = adZone;

        gameObject.SetActive(true);

        if (cancelButtonVisible)
            backToken = NavigateBackStack.Instance.Push(() =>
            {
                SetResult(Result.Cancel);
                return true;
            });
        else
            backToken = NavigateBackStack.Instance.Push(() =>
            {
                SetResult(Result.No);
                return true;
            });

        return resultTcs.Task;
    }

    public Task<Result> Show(string text, string yesButtonText, string noButtonText, bool withCancelButton, AdRepository.AdZone adZone) =>
        Show(text, yesButtonText, noButtonText, true, withCancelButton, adZone);

    public Task<Result> Show(string text, string yesButtonText, string noButtonText, bool withCancelButton) =>
        Show(text, yesButtonText, noButtonText, true, withCancelButton, null);

    public Task<Result> Show(string text, string yesButtonText, string noButtonText, AdRepository.AdZone adZone) =>
        Show(text, yesButtonText, noButtonText, true, false, adZone);

    public Task<Result> Show(string text, string yesButtonText, string noButtonText) =>
        Show(text, yesButtonText, noButtonText, true, false, null);

    public Task Show(string text, string yesButtonText, AdRepository.AdZone adZone) =>
        Show(text, yesButtonText, "", false, false, adZone);

    public Task Show(string text, string yesButtonText) =>
        Show(text, yesButtonText, "", false, false, null);

    void SetResult(Result result)
    {
        var tcs = resultTcs;

        resultTcs = null;
        gameObject.SetActive(false);

        NavigateBackStack.Instance.Remove(backToken);

        tcs.TrySetResult(result);
    }

    public void Yes() => SetResult(Result.Yes);

    public void No() => SetResult(Result.No);

    public void Cancel() => SetResult(Result.Cancel);
}
