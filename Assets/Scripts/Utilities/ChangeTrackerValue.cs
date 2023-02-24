using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChangeTrackerValue<T>
{
    public event Action<T> ValueChanged;

    public T Value { get; private set; }

    public void Update(T value)
    {
        if (!Value.Equals(value))
        {
            Value = value;
            ValueChanged?.Invoke(value);
        }
    }
}
