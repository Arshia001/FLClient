using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataStore
{
    static DataStore instance;
    public static DataStore Instance
    {
        get
        {
            if (instance == null)
                instance = new DataStore();
            return instance;
        }
    }

    internal ChangeNotifier<Guid?> ClientID { get; } = new ChangeNotifier<Guid?>();

    internal ChangeNotifier<bool> HaveBazaarToken { get; } = new ChangeNotifier<bool>();

    internal ChangeNotifier<bool> SoundEnabled { get; } = new ChangeNotifier<bool>();

    DataStore()
    {
        LoadAll();
        RegisterChangeEvents();
    }

    void RegisterChangeEvents()
    {
        ClientID.ValueChanged += (value) =>
        {
            PlayerPrefs.SetString(nameof(ClientID), value?.ToString() ?? "");
            PlayerPrefs.Save();
        };

        HaveBazaarToken.ValueChanged += (value) =>
        {
            PlayerPrefs.SetInt(nameof(HaveBazaarToken), value ? 1 : 0);
            PlayerPrefs.Save();
        };

        SoundEnabled.ValueChanged += (value) =>
        {
            PlayerPrefs.SetInt(nameof(SoundEnabled), value ? 1 : 0);
            PlayerPrefs.Save();
        };
    }

    void LoadAll()
    {
        using (ChangeNotifier.BeginBatch(true))
        {
            ClientID.Value = Guid.TryParse(PlayerPrefs.GetString(nameof(ClientID), ""), out var guid) ? guid : default(Guid?);
            HaveBazaarToken.Value = PlayerPrefs.GetInt(nameof(HaveBazaarToken), 0) == 0 ? false : true;
            SoundEnabled.Value = PlayerPrefs.GetInt(nameof(SoundEnabled), 1) == 0 ? false : true;
        }
    }
}
