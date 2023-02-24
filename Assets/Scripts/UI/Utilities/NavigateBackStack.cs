using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Linq;
using UnityEngine;

public class NavigateBackStack : SingletonBehaviour<NavigateBackStack>
{
    SortedList<int, Func<bool>> callbacks = new SortedList<int, Func<bool>>();

    int nextToken = 0;

    public int Push(Func<bool> f)
    {
        var token = ++nextToken;
        callbacks.Add(token, f);
        return token;
    }

    public void Remove(int token)
    {
        callbacks.Remove(token);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && callbacks.Count > 0)
        {
            var token = callbacks.Keys.Last();
            var f = callbacks[token];

            if (f())
            {
                SoundEffectManager.Play(SoundEffect.BackButtonPress);
                callbacks.Remove(token);
            }
        }
    }
}
