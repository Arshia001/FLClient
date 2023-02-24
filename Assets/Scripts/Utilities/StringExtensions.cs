using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class StringExtensions
{
    public static string CapLength(this string s, int maxLength) => 
        s.Length <= maxLength ? s : s.Substring(0, maxLength);
}
