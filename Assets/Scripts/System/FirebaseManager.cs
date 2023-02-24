using Firebase;
using Firebase.Messaging;
using GameAnalyticsSDK;
using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class FirebaseManager : SingletonBehaviour<FirebaseManager>
{
    protected override bool IsGlobal => true;

#pragma warning disable IDE0052 // Remove unread private members - The Firebase documentation mentions holding a reference to the app
    FirebaseApp app;
#pragma warning restore IDE0052 // Remove unread private members

    public string Token { get; private set; }

    void Start() => TaskExtensions.RunIgnoreAsync(async () =>
    {
        var checkResult = await FirebaseApp.CheckAndFixDependenciesAsync();
        if (checkResult == DependencyStatus.Available)
        {
            app = FirebaseApp.DefaultInstance;

            InitializeMessaging();
        }
    });

    void InitializeMessaging()
    {
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;
    }

    void OnTokenReceived(object sender, TokenReceivedEventArgs e)
    {
        Token = e.Token;
        Debug.Log("Received firebase token: " + Token);
        if (ConnectionManager.Instance.IsConnected)
            ConnectionManager.Instance.EndPoint<SystemEndPoint>().RegisterFcmToken(e.Token);
    }

    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        try
        {
            if (e.Message.NotificationOpened)
            {
                if (e.Message.Data != null &&
                    e.Message.Data.TryGetValue("analytics_category", out var category))
                {
                    category = category.Length > 16 ? category.Substring(0, 16) : category;
                    GameAnalytics.NewDesignEvent($"pushnotifopen:{category}");
                }
                else
                    GameAnalytics.NewDesignEvent($"pushnotifopen:UNKNOWN");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to process Firebase message due to exception:\n{ex}");
        }
    }
}
