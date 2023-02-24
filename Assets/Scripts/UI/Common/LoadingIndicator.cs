using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LoadingIndicator : SingletonBehaviour<LoadingIndicator>
{
    GameObject clickBlocker;

    List<bool> activations = new List<bool>();

    int backToken = -1;

    protected override void Awake()
    {
        base.Awake();

        clickBlocker = transform.Find("ClickBlocker").gameObject;

        gameObject.SetActive(false);
    }

    public static IDisposable Show(bool blockClicks) => Instance.ShowInternal(blockClicks);

    IDisposable ShowInternal(bool blockClicks)
    {
        activations.Add(blockClicks);

        gameObject.SetActive(true);

        var shouldBlockClicks = activations.Any(t => t);

        clickBlocker.SetActive(shouldBlockClicks);

        if (shouldBlockClicks && backToken == -1)
            backToken = NavigateBackStack.Instance.Push(() => false);

        return new LoadingIndicatorHider();
    }

    void HideOne()
    {
        if (activations.Count > 0)
            activations.RemoveAt(activations.Count - 1);

        gameObject.SetActive(activations.Count > 0);

        var shouldBlockClicks = activations.Any(t => t);

        clickBlocker.SetActive(shouldBlockClicks);

        if (!shouldBlockClicks && backToken >= 0)
        {
            NavigateBackStack.Instance.Remove(backToken);
            backToken = -1;
        }
    }

    sealed class LoadingIndicatorHider : IDisposable
    {
        bool disposed = false;

        public void Dispose()
        {
            if (!disposed)
            {
                Instance.HideOne();
                disposed = true;
            }
        }
    }
}
