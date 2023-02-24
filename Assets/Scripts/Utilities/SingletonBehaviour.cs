using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    public static T Instance { get; private set; }


    protected virtual bool IsGlobal => false;


    protected virtual void Awake()
    {
        if (IsGlobal)
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Instance = (T)this;
    }

    protected virtual void OnDestroy()
    {
        if (!IsGlobal && Instance == this)
            Instance = null;
    }
}
