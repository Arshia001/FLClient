using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ObjectExtensions
{
    public static void ApplyIfNotNull<T>(this T obj, Action<T> f)
        where T : class
    {
        if (obj != null)
            f(obj);
    }

    public static TResult ApplyIfNotNull<T, TResult>(this T obj, Func<T, TResult> f)
        where T : class
        where TResult : class
    {
        if (obj != null)
            return f(obj);

        return null;
    }

    public static T Apply<T>(this T val, Action<T> act)
    {
        act(val);
        return val;
    }
}
