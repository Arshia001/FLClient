using Network;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class AndroidShare
{
    public static void Share(string subject, string text)
    {
        var intentClass = new AndroidJavaClass("android.content.Intent");
        var intentObject = new AndroidJavaObject("android.content.Intent");
        intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));

        intentObject.Call<AndroidJavaObject>("setType", "text/plain");
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_SUBJECT"), subject);
        intentObject.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

        var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var currentActivity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intentObject, "به‌اشتراک‌گذاری");
        currentActivity.Call("startActivity", chooser);

        ConnectionManager.Instance.DelayKeepAlive(TimeSpan.FromMinutes(5));
    }
}
