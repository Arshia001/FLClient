using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static UnityEngine.Mathf;

public class ToastBase<T> : SingletonBehaviour<T> where T : ToastBase<T>
{
    class MessageInfo
    {
        public Transform transform;
        public CanvasGroup canvasGroup;
        public float progress;
        public string text;

        public MessageInfo(Transform tr, CanvasGroup group, float progress, string text)
        {
            this.transform = tr;
            this.canvasGroup = group;
            this.progress = progress;
            this.text = text;
        }
    }

    static ConcurrentQueue<string> queuedMessages = new ConcurrentQueue<string>();

    public static void ShowWhenAvailable(string message)
    {
        if (Instance == null)
            queuedMessages.Enqueue(message);
        else
            Instance.Enqueue(message);
    }

    [SerializeField] float progressToShowNext = 0.8f;
    [SerializeField] float itemDuration = 1.2f;
    [SerializeField] float fadeInterval = 0.25f;
    [SerializeField] float minScale = 0.6f;
    [SerializeField] float moveAmount = 10f;

    Queue<string> nextMessages = new Queue<string>();
    Queue<MessageInfo> activeMessages = new Queue<MessageInfo>();
    GameObject template;

    public bool IsShowingToast => activeMessages.Any() && activeMessages.Min(m => m.progress) < progressToShowNext;

    protected override void Awake()
    {
        base.Awake();

        template = transform.Find("Template").gameObject;
    }

    void OnEnable()
    {
        while (queuedMessages.TryDequeue(out var message))
            Enqueue(message);
    }

    void SpawnMessage(string message)
    {
        var tr = transform.AddListItem(template);

        Translation.SetTextNoTranslate(tr.GetComponentInChildren<TextMeshProUGUI>(), message);

        var item = new MessageInfo(tr, tr.GetComponentInChildren<CanvasGroup>(), 0.0f, message);
        activeMessages.Enqueue(item);
        UpdateItem(item);
    }

    void UpdateItem(MessageInfo item)
    {
        item.progress = MoveTowards(item.progress, 1.0f, Time.deltaTime / itemDuration);

        var fade = Clamp01((0.5f - Abs(0.5f - item.progress)) / fadeInterval);
        item.transform.localScale = Vector3.one * Lerp(minScale, 1.0f, fade);
        item.canvasGroup.alpha = fade;
        item.transform.localPosition = template.transform.localPosition + Vector3.up * (item.progress - 0.5f) * moveAmount;
    }

    void DestroyItem(MessageInfo item)
    {
        Destroy(item.transform.gameObject);
    }

    void Update()
    {
        float minProgress = 1.0f;

        foreach (var item in activeMessages)
        {
            UpdateItem(item);
            if (item.progress < minProgress)
                minProgress = item.progress;

            if (item.progress >= 1)
                DestroyItem(item);
        }

        while (activeMessages.Any() && activeMessages.Peek().progress >= 1.0f)
            activeMessages.Dequeue();

        if (minProgress > progressToShowNext && nextMessages.Any())
            SpawnMessage(nextMessages.Dequeue());
    }

    public void Enqueue(string message)
    {
        if (nextMessages.Any(s => s == message) || activeMessages.Any(m => m.text == message && m.progress < progressToShowNext))
            return;
        if (activeMessages.Any())
            nextMessages.Enqueue(message);
        else
            SpawnMessage(message);
    }
}
