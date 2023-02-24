using Network;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProfileHelper
{
    public static void ChangeClientID(Guid? id)
    {
        DataStore.Instance.ClientID.Value = id;
        ConnectionManager.Instance.Disconnect(); // This will trigger a load of the startup scene
    }

    public static void LogOut()
    {
        DataStore.Instance.HaveBazaarToken.Value = false;
        ChangeClientID(null);
    }
}
