using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameAnalyticsStarter : MonoBehaviour
{
    void Start() => GameAnalyticsSDK.GameAnalytics.Initialize();
}
