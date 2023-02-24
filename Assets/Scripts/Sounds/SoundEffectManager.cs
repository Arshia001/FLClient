using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundEffect
{
    BackButtonPress,
    ButtonPress,
    ExtendTimePowerup,
    GainCoins,
    RoundTimeFinished,
    Word_Pending,
    Word_0Score,
    Word_1Score,
    Word_2Score,
    Word_3Score,
}

public class SoundEffectManager : SingletonBehaviour<SoundEffectManager>
{
    [SerializeField] AudioClip backButtonPress = default;
    [SerializeField] AudioClip buttonPress = default;
    [SerializeField] AudioClip extendTimePowerup = default;
    [SerializeField] AudioClip gainCoins = default;
    [SerializeField] AudioClip roundTimeFinished = default;
    [SerializeField] AudioClip word_Pending = default;
    [SerializeField] AudioClip word_0Score = default;
    [SerializeField] AudioClip word_1Score = default;
    [SerializeField] AudioClip word_2Score = default;
    [SerializeField] AudioClip word_3Score = default;

    protected override bool IsGlobal => true;

    public static void Play(SoundEffect se)
    {
        if (!DataStore.Instance.SoundEnabled)
            return;

        var clip = Instance.GetClip(se);
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    AudioClip GetClip(SoundEffect se)
    {
        switch (se)
        {
            case SoundEffect.BackButtonPress: return backButtonPress;
            case SoundEffect.ButtonPress: return buttonPress;
            case SoundEffect.ExtendTimePowerup: return extendTimePowerup;
            case SoundEffect.GainCoins: return gainCoins;
            case SoundEffect.RoundTimeFinished: return roundTimeFinished;
            case SoundEffect.Word_Pending: return word_Pending;
            case SoundEffect.Word_0Score: return word_0Score;
            case SoundEffect.Word_1Score: return word_1Score;
            case SoundEffect.Word_2Score: return word_2Score;
            case SoundEffect.Word_3Score: return word_3Score;
            default: return null;
        }
    }
}
