using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class CollectionExtensions
{
    public static void Iterate<T>(this IEnumerable<T> ts, Action<T> f)
    {
        foreach (var t in ts)
            f(t);
    }
}
