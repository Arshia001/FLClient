using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class TaskExtensions
{
    private static readonly Action<Task> IgnoreTaskContinuation = t =>
    {
        var ex = t.Exception;
        if (ex != null)
            Debug.LogException(ex);
    };

    public static void Ignore(this Task task)
    {
        if (task.IsCompleted)
        {
            var ex = task.Exception;
            if (ex != null)
                Debug.LogException(ex);
        }
        else
        {
            task.ContinueWith(
                IgnoreTaskContinuation,
                CancellationToken.None,
                TaskContinuationOptions.NotOnRanToCompletion | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    public static void RunIgnoreAsync(Func<Task> func) => func().Ignore();

    public static IEnumerator RunAsCoroutine(this Task task)
    {
        while (task.Status == TaskStatus.Running)
            yield return null;

        if (task.Status == TaskStatus.Faulted || task.Status == TaskStatus.Canceled)
            throw task.Exception;
    }
}
