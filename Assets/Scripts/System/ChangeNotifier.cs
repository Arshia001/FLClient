using System;
using System.Collections.Generic;
using System.Linq;

abstract class ChangeNotifier
{
    protected class ChangeNotificationBatch
    {
        public class BatchFinisher : IDisposable
        {
            ChangeNotificationBatch owner;

            public BatchFinisher(ChangeNotificationBatch owner) => this.owner = owner;

            public void Dispose() => owner.EndBatch();
        }

        static ChangeNotificationBatch current;
        static List<(ChangeNotifier v, bool dontNotify)> currentBatchChanges = new List<(ChangeNotifier v, bool dontNotify)>();
        static bool dontNotify;

        public static bool IsInBatch => current != null;

        public static void Track(ChangeNotifier changeNotifier)
        {
            currentBatchChanges.Add((changeNotifier, dontNotify));
        }

        ChangeNotificationBatch previous;
        bool dontNotifyInst;

        public ChangeNotificationBatch(bool dontNotify)
        {
            if (current != null)
                previous = current;

            current = this;
            dontNotifyInst = ChangeNotificationBatch.dontNotify = dontNotify;
        }

        void EndBatch()
        {
            if (previous != null)
            {
                current = previous;
                dontNotify = previous.dontNotifyInst;
            }
            else
            {
                current = null;
                dontNotify = false;
                foreach (var (v, dontNotify) in currentBatchChanges)
                    v.DoChange(!dontNotify);
                currentBatchChanges.Clear();
            }
        }
    }

    public static IDisposable BeginBatch(bool dontNotify = false)
    {
        var newBatch = new ChangeNotificationBatch(dontNotify);
        return new ChangeNotificationBatch.BatchFinisher(newBatch);
    }

    protected abstract void DoChange(bool notify);
}

class ChangeNotifier<T> : ChangeNotifier
{
    static Func<T, T, bool> equalityDelegate;

    public static implicit operator T(ChangeNotifier<T> self) => self.value;

    static ChangeNotifier()
    {
        if (typeof(T).GetInterfaces().Contains(typeof(IEquatable<T>)))
            equalityDelegate = (t1, t2) => t1 == null && t2 == null || t1 != null && t2 != null && (t1 as IEquatable<T>).Equals(t2);
        else
            equalityDelegate = (t1, t2) => Equals(t1, t2);
    }

    public delegate void ValueChangedDelegate(T newValue);
    public event ValueChangedDelegate ValueChanged;

    T valueAfterBatch;
    T value;
    public T Value
    {
        get => value;

        set
        {
            if (ChangeNotificationBatch.IsInBatch)
            {
                valueAfterBatch = value;
                ChangeNotificationBatch.Track(this);
            }
            else if (!equalityDelegate(this.value, value))
            {
                SetValue(value, true);
            }
        }
    }

    public override string ToString() => value?.ToString();

    void SetValue(T value, bool notify)
    {
        this.value = value;
        if (notify)
            ValueChanged?.Invoke(this.value);
    }

    protected override void DoChange(bool notify)
    {
        if (!equalityDelegate(valueAfterBatch, value))
            SetValue(valueAfterBatch, notify);
    }
}
