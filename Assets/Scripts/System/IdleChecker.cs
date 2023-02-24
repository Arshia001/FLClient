using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IdleChecker : SingletonBehaviour<IdleChecker>
{
    const float timeoutSeconds = 5 * 60;

    float lastInteractionTime;

    public bool IsIdle => Time.time - lastInteractionTime > timeoutSeconds;

    protected override bool IsGlobal => true;

    void Update()
    {
        if (Input.touchCount > 0 || Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Escape))
            lastInteractionTime = Time.time;

        if (IsIdle)
            DisconnectIfNeeded();
    }

    void DisconnectIfNeeded()
    {
        var cm = ConnectionManager.Instance;
        if (SceneManager.GetActiveScene().buildIndex > 0 && cm.IsConnectedOrReconnecting)
            cm.Disconnect();
    }
}
