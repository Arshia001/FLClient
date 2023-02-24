using Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisconnectionMarker : MonoBehaviour
{
    ConnectionManager connectionManager;
    Image image;

    void Start()
    {
        connectionManager = ConnectionManager.Instance;
        image = GetComponentInChildren<Image>();
    }

    void Update()
    {
        image.gameObject.SetActive(!connectionManager.IsConnected);
        image.color = new Color(1, 1, 1, (Mathf.Sin(Time.time * 4) + 1) / 2);
    }
}
