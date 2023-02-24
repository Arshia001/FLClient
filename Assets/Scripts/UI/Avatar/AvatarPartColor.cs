using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarPartColor : MonoBehaviour
{
    [SerializeField] Color color = default;

    public Color Color => color;
}
