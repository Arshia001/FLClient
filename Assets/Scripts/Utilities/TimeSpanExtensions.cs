using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class TimeSpanExtensions
{
    public static string FormatAsPersianExpression(this TimeSpan timeSpan, bool excludeSeconds = false)
    {
        var parts = new List<string>();

        if (timeSpan.Days > 0)
            parts.Add($"{timeSpan.Days} روز");
        if (timeSpan.Hours > 0)
            parts.Add($"{timeSpan.Hours} ساعت");
        if (timeSpan.Minutes > 0)
            parts.Add($"{timeSpan.Minutes} دقیقه");
        if (!excludeSeconds && timeSpan.Seconds > 0)
            parts.Add($"{timeSpan.Seconds} ثانیه");

        return parts.Count == 0 ? "صفر ثانیه" : string.Join(" و ", parts);
    }

    public static string FormatAsClock(this TimeSpan timeSpan)
    {
        var hours = timeSpan.Days * 24 + timeSpan.Hours;
        return $"{PersianTextShaper.PersianTextShaper.ShapeText(hours.ToString())}:" +
            $"{PersianTextShaper.PersianTextShaper.ShapeText(timeSpan.Minutes.ToString("00"))}:" +
            $"{PersianTextShaper.PersianTextShaper.ShapeText(timeSpan.Seconds.ToString("00"))}";
    }
}
