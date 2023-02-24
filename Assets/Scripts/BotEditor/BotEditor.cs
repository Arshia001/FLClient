using Network.Types;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class BotEditor : MonoBehaviour
{
    const string filePath = "bots.json";

    [SerializeField] string avatarConfigJson = default;

    AvatarDisplay avatar;
    List<BotConfig> bots = new List<BotConfig>();
    int editIndex;

    void Awake() => avatar = GetComponentInChildren<AvatarDisplay>();

    void Start()
    {
        var config = JsonConvert.DeserializeObject<AvatarConfig>(avatarConfigJson);
        var parts = config.GetIndexedData();
        AvatarPartRepository.Instance.Initialize(
            parts
            .SelectMany(p => p.Value.Values)
            .Select(a => new AvatarPartConfigDTO(a.Type, a.ID, a.Price, a.MinimumLevel)));

        AddNew();
    }

    void RefreshAvatar()
    {
        var parts = new List<AvatarPartDTO>();

        parts.Add(new AvatarPartDTO(AvatarPartType.HeadShape, bots[editIndex].AvatarHeadShape));
        parts.Add(new AvatarPartDTO(AvatarPartType.Eyes, bots[editIndex].AvatarEyes));
        parts.Add(new AvatarPartDTO(AvatarPartType.Mouth, bots[editIndex].AvatarMouth));
        if (bots[editIndex].AvatarHair.HasValue)
            parts.Add(new AvatarPartDTO(AvatarPartType.Hair, bots[editIndex].AvatarHair.Value));
        if (bots[editIndex].AvatarGlasses.HasValue)
            parts.Add(new AvatarPartDTO(AvatarPartType.Glasses, bots[editIndex].AvatarGlasses.Value));

        avatar.SetAvatar(new AvatarDTO(parts));
    }

    void SetEditIndex(int index)
    {
        editIndex = index;
        RefreshAvatar();
    }

    void MoveEditIndex(int delta)
    {
        editIndex += delta;
        while (editIndex < 0)
            editIndex += bots.Count;
        SetEditIndex(editIndex % bots.Count);
    }

    void AddNew()
    {
        bots.Add(new BotConfig(bots.Count == 0 ? 1 : bots.Last().ID + 1));
        SetEditIndex(bots.Count - 1);
    }

    void RemoveCurrent()
    {
        bots.RemoveAt(editIndex);
        if (bots.Count == 0)
            AddNew();
        else if (editIndex >= bots.Count)
            editIndex = bots.Count - 1;
    }

    ushort GetSelectedPart(AvatarPartType partType)
    {
        switch (partType)
        {
            case AvatarPartType.Eyes:
                return bots[editIndex].AvatarEyes;
            case AvatarPartType.Glasses:
                return bots[editIndex].AvatarGlasses ?? 0;
            case AvatarPartType.Hair:
                return bots[editIndex].AvatarHair ?? 0;
            case AvatarPartType.HeadShape:
                return bots[editIndex].AvatarHeadShape;
            case AvatarPartType.Mouth:
                return bots[editIndex].AvatarMouth;
            default:
                throw new System.Exception("Unknown avatar part type");
        }
    }

    void SetSelectedPart(AvatarPartType partType, ushort? id)
    {
        switch (partType)
        {
            case AvatarPartType.Eyes:
                bots[editIndex].AvatarEyes = id.Value;
                return;
            case AvatarPartType.Glasses:
                bots[editIndex].AvatarGlasses = id;
                return;
            case AvatarPartType.Hair:
                bots[editIndex].AvatarHair = id;
                return;
            case AvatarPartType.HeadShape:
                bots[editIndex].AvatarHeadShape = id.Value;
                return;
            case AvatarPartType.Mouth:
                bots[editIndex].AvatarMouth = id.Value;
                return;
            default:
                throw new System.Exception("Unknown avatar part type");
        }
    }

    void MoveAvatarPartSelection(AvatarPartType partType, int delta)
    {
        var all = AvatarPartRepository.Instance.GetPartIDs(partType).ToList();
        var current = all.IndexOf(GetSelectedPart(partType));

        var firstIndex = partType == AvatarPartType.Glasses || partType == AvatarPartType.Hair ? -1 : 0;

        current += delta;
        if (current >= all.Count)
            current = firstIndex;
        if (current < firstIndex)
            current = all.Count - 1;

        SetSelectedPart(partType, current >= 0 ? all[current] : default(ushort?));

        RefreshAvatar();
    }

    void AvatarPartInput(AvatarPartType partType)
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button($"-10 {partType}", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            MoveAvatarPartSelection(partType, -10);
        if (GUILayout.Button($"-1 {partType}", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            MoveAvatarPartSelection(partType, -1);
        if (GUILayout.Button($"{partType} +1", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            MoveAvatarPartSelection(partType, 1);
        if (GUILayout.Button($"{partType} +10", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            MoveAvatarPartSelection(partType, 10);

        GUILayout.EndHorizontal();
    }

    void Load()
    {
        if (!File.Exists(filePath))
            return;
            
        bots = JsonConvert.DeserializeObject<List<BotConfig>>(File.ReadAllText(filePath));
        editIndex = bots.Count - 1;
        RefreshAvatar();
    }

    void Save() =>
        File.WriteAllText(filePath, JsonConvert.SerializeObject(bots, Formatting.Indented));

    void OnGUI()
    {
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Add new", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            AddNew();

        if (GUILayout.Button("< Prev", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            MoveEditIndex(-1);
        if (GUILayout.Button("Next >", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            MoveEditIndex(1);

        if (GUILayout.Button("Remove", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            RemoveCurrent();

        GUILayout.EndHorizontal();

        GUILayout.Label($"ID: {bots[editIndex].ID}");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Name: ");
        bots[editIndex].Name = GUILayout.TextField(bots[editIndex].Name, GUILayout.MinWidth(400));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Level: {bots[editIndex].Level}");
        bots[editIndex].Level = (uint)Mathf.RoundToInt(GUILayout.HorizontalSlider(bots[editIndex].Level, 1, 20, GUILayout.MinWidth(400)));
        GUILayout.EndHorizontal();

        AvatarPartInput(AvatarPartType.HeadShape);
        AvatarPartInput(AvatarPartType.Hair);
        AvatarPartInput(AvatarPartType.Eyes);
        AvatarPartInput(AvatarPartType.Glasses);
        AvatarPartInput(AvatarPartType.Mouth);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Open...", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            Load();
        if (GUILayout.Button("Save...", GUILayout.MinHeight(50), GUILayout.MinWidth(100)))
            Save();

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    public class AvatarConfig
    {
        public AvatarConfig(List<TypelessAvatarPartConfig> headShapes, List<TypelessAvatarPartConfig> hairs,
            List<TypelessAvatarPartConfig> eyes, List<TypelessAvatarPartConfig> mouths,
            List<TypelessAvatarPartConfig> glasses)
        {
            HeadShapes = headShapes;
            Hairs = hairs;
            Eyes = eyes;
            Mouths = mouths;
            Glasses = glasses;
        }

        public List<TypelessAvatarPartConfig> HeadShapes { get; private set; }
        public List<TypelessAvatarPartConfig> Hairs { get; private set; }
        public List<TypelessAvatarPartConfig> Eyes { get; private set; }
        public List<TypelessAvatarPartConfig> Mouths { get; private set; }
        public List<TypelessAvatarPartConfig> Glasses { get; private set; }

        public Dictionary<AvatarPartType, Dictionary<ushort, AvatarPartConfig>> GetIndexedData()
        {
            Dictionary<ushort, AvatarPartConfig> ToDictionary(IEnumerable<TypelessAvatarPartConfig> parts, AvatarPartType type) =>
                parts.ToDictionary(p => p.ID, p => new AvatarPartConfig(p, type));

            return new Dictionary<AvatarPartType, Dictionary<ushort, AvatarPartConfig>>()
            {
                { AvatarPartType.HeadShape, ToDictionary(HeadShapes, AvatarPartType.HeadShape) },
                { AvatarPartType.Hair, ToDictionary(Hairs, AvatarPartType.Hair) },
                { AvatarPartType.Eyes, ToDictionary(Eyes, AvatarPartType.Eyes) },
                { AvatarPartType.Mouth, ToDictionary(Mouths, AvatarPartType.Mouth) },
                { AvatarPartType.Glasses, ToDictionary(Glasses, AvatarPartType.Glasses) },
            };
        }
    }

    public class TypelessAvatarPartConfig
    {
        public TypelessAvatarPartConfig(ushort id, uint price, ushort minimumLevel)
        {
            ID = id;
            Price = price;
            MinimumLevel = minimumLevel;
        }

        public ushort ID { get; }
        public uint Price { get; }
        public ushort MinimumLevel { get; }
    }

    public class AvatarPartConfig
    {
        public AvatarPartConfig(ushort id, uint price, ushort minimumLevel, AvatarPartType type)
        {
            ID = id;
            Price = price;
            Type = type;
            MinimumLevel = minimumLevel;
        }

        public AvatarPartConfig(TypelessAvatarPartConfig jsonConfig, AvatarPartType type)
            : this(jsonConfig.ID, jsonConfig.Price, jsonConfig.MinimumLevel, type) { }

        public ushort ID { get; }
        public uint Price { get; }
        public ushort MinimumLevel { get; }
        public AvatarPartType Type { get; }
    }

    public class BotConfig
    {
        public BotConfig(int id)
        {
            ID = id;
            Name = "";
            Level = 1;
            AvatarHeadShape = 1;
            AvatarMouth = 1;
            AvatarEyes = 1;
            AvatarHair = null;
            AvatarGlasses = null;
        }

        public int ID { get; set; }
        public string Name { get; set; }
        public uint Level { get; set; }
        public ushort AvatarHeadShape { get; set; }
        public ushort AvatarMouth { get; set; }
        public ushort AvatarEyes { get; set; }
        public ushort? AvatarHair { get; set; }
        public ushort? AvatarGlasses { get; set; }
    }
}
