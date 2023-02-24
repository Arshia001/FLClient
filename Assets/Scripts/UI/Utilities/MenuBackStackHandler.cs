using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuBackStackHandler
{
    int? token = null;
    Func<bool> f;

    public MenuBackStackHandler(Func<bool> f) => this.f = f;

    public void MenuShown()
    {
        if (!token.HasValue)
            token = NavigateBackStack.Instance.Push(f);
    }

    public void MenuHidden()
    {
        if (token.HasValue)
        {
            NavigateBackStack.Instance.Remove(token.Value);
            token = null;
        }
    }
}
