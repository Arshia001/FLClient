using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameAnalyticsSDK;
using Network;
using Network.Types;
using UnityEngine;

public partial class TutorialManager : SingletonBehaviour<TutorialManager>
{
#pragma warning disable CS0414
    [SerializeField] bool debug_skipIntroGameInEditor = true;
#pragma warning restore CS0414

    BitArray progress;

    TutorialBackground background;
    TutorialHighlight highlight;
    TutorialMessageBox messageBoxTop, messageBoxBottom;
    TutorialArrow arrow;

    event Action HighlightTapped;
    event Action OKTapped;

    public bool TutorialInProgress { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        progress = new BitArray(BitConverter.GetBytes(TransientData.Instance.TutorialProgress));

        background = transform.Find("Background").GetComponent<TutorialBackground>();
        highlight = transform.Find("Highlight").GetComponent<TutorialHighlight>();
        messageBoxTop = transform.Find("MessageBoxTop").GetComponent<TutorialMessageBox>();
        messageBoxBottom = transform.Find("MessageBoxBottom").GetComponent<TutorialMessageBox>();
        arrow = transform.Find("Arrow").GetComponent<TutorialArrow>();
    }

    void Start()
    {
        HideAll();

        transform.Find("Game").gameObject.SetActive(false);
        if (!GetStepComplete(TutorialStep.IntroductoryGame))
        {
#if UNITY_EDITOR
            if (debug_skipIntroGameInEditor)
                return;
#endif
            StartIntroGame();
        }
    }

    bool GetStepComplete(TutorialStep step) => progress.Get((int)step);

    void SetStepComplete(TutorialStep step) => TaskExtensions.RunIgnoreAsync(async () =>
    {
        progress.Set((int)step, true);

        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "tutorial", step.ToString());

        var bytes = new byte[8];
        progress.CopyTo(bytes, 0);
        var asULong = BitConverter.ToUInt64(bytes, 0);
        await ConnectionManager.Instance.EndPoint<SystemEndPoint>().SetTutorialProgress(asULong);
    });

    void HideAll()
    {
        background.Hide();
        highlight.Hide();
        messageBoxTop.Hide();
        messageBoxBottom.Hide();
        arrow.Hide();
    }

    int StartTutorialSequence()
    {
        AdRepository.Instance.AdsEnabled = false;
        TutorialInProgress = true;
        return NavigateBackStack.Instance.Push(() => false);
    }

    void FinishTutorialSequence(int backStackToken)
    {
        AdRepository.Instance.AdsEnabled = true;
        TutorialInProgress = false;
        NavigateBackStack.Instance.Remove(backStackToken);
    }

    void ShowHighlight(RectTransform target) => highlight.Show(target);

    void ShowBackground() => background.Show();

    void ShowMessageBoxTop(string message, bool showOKButton) => messageBoxTop.Show(message, showOKButton);

    void ShowMessageBoxBottom(string message, bool showOKButton) => messageBoxBottom.Show(message, showOKButton);

    void ShowArrow(RectTransform target, TutorialArrow.Direction direction) => arrow.Show(target, direction);

    public void HighlightedAreaTapped() => HighlightTapped?.Invoke();

    public void OKButtonTapped() => OKTapped?.Invoke();

    IEnumerator WaitForHighlightTap()
    {
        var tapped = false;
        var startTime = Time.time;

        Action act = () =>
        {
            if (Time.time - startTime > 0.2f)
                tapped = true;
        };
        HighlightTapped += act;

        while (!tapped)
            yield return null;

        HighlightTapped -= act;

        SoundEffectManager.Play(SoundEffect.ButtonPress);
    }

    IEnumerator WaitForOKButtonTap()
    {
        bool tapped = false;
        var startTime = Time.time;

        Action act = () =>
        {
            if (Time.time - startTime > 0.2f)
                tapped = true;
        };
        OKTapped += act;

        while (!tapped)
            yield return null;

        OKTapped -= act;

        SoundEffectManager.Play(SoundEffect.ButtonPress);
    }

    IEnumerator ShowMessageBoxBottomAndWait(string message)
    {
        HideAll();
        ShowBackground();
        ShowMessageBoxBottom(message, true);
        yield return WaitForOKButtonTap();
        HideAll();
    }

    IEnumerator HighlightAndWait(RectTransform target, TutorialArrow.Direction arrowDirection)
    {
        HideAll();
        ShowHighlight(target);
        ShowArrow(target, arrowDirection);
        yield return WaitForHighlightTap();
        HideAll();
    }

    IEnumerator HighlightWithMessageTopAndWait(RectTransform target, TutorialArrow.Direction? arrowDirection, string message, bool shouldTapHighlight)
    {
        HideAll();
        ShowHighlight(target);
        ShowMessageBoxTop(message, !shouldTapHighlight);
        if (arrowDirection.HasValue)
            ShowArrow(target, arrowDirection.Value);
        yield return shouldTapHighlight ? WaitForHighlightTap() : WaitForOKButtonTap();
        HideAll();
    }

    IEnumerator HighlightWithMessageBottomAndWait(RectTransform target, TutorialArrow.Direction? arrowDirection, string message, bool shouldTapHighlight)
    {
        HideAll();
        ShowHighlight(target);
        ShowMessageBoxBottom(message, !shouldTapHighlight);
        if (arrowDirection.HasValue)
            ShowArrow(target, arrowDirection.Value);
        yield return shouldTapHighlight ? WaitForHighlightTap() : WaitForOKButtonTap();
        HideAll();
    }

    public bool BackToMainMenu()
    {
        var td = TransientData.Instance;
        if (!GetStepComplete(TutorialStep.CreateAccount) && td.RegistrationStatus == RegistrationStatus.Unregistered && td.Level.Value >= 2)
        {
            StartCoroutine(RunShowAccountSettings());
            return true;
        }
        else if (!GetStepComplete(TutorialStep.CreateAccount) && td.RegistrationStatus == RegistrationStatus.BazaarToken && td.Level.Value >= 2)
        {
            StartCoroutine(RunEditBazaarAccountUserName());
            return true;
        }
        else if (!GetStepComplete(TutorialStep.EditAvatar) && td.GetStatisticValue(Statistics.RoundsCompleted) >= 6)
        {
            StartCoroutine(RunShowEditAvatar());
            return true;
        }
        else if (!GetStepComplete(TutorialStep.CreateSubject) && td.Level.Value >= 3)
        {
            StartCoroutine(RunShowCreateSubject());
            return true;
        }
        return false;
    }

    enum TutorialStep
    {
        IntroductoryGame = 0,
        RoundInfo = 1,
        UpgradedActiveGameLimitMode = 2,
        GroupChange = 3,
        CreateAccount = 4,
        CreateSubject = 5,
        EditAvatar = 6
    }
}
