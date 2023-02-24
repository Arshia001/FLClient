using Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectionHandler : SingletonBehaviour<DisconnectionHandler>
{
    protected override bool IsGlobal => true;

    void Start() => ConnectionManager.Instance.SessionTerminated += ConnectionManager_SessionTerminated;

    private void ConnectionManager_SessionTerminated(bool wasCleanShutdown)
    {
        if (SceneManager.GetActiveScene().buildIndex != 0 && Application.isPlaying)
            SceneManager.LoadScene(0);
    }
}
