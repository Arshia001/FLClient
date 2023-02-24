using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AvatarPartDefinition
{
    public AvatarPartDefinition(ushort id, int sprite, int color)
    {
        ID = id;
        GraphicsDefinitionID = sprite;
        ColorID = color;
    }

    [JsonProperty("id")]
    public ushort ID { get; }

    [JsonProperty("sprite")]
    public int GraphicsDefinitionID { get; }

    [JsonProperty("color")]
    public int ColorID { get; }

    public static Dictionary<ushort, AvatarPartDefinition> Read(string resourcePath)
    {
        var textAsset = Resources.Load<TextAsset>(resourcePath);

        if (textAsset != null)
        {
            var list = JsonConvert.DeserializeObject<List<AvatarPartDefinition>>(textAsset.text);

            Resources.UnloadAsset(textAsset);
            return list.ToDictionary(d => d.ID);
        }
        else
            throw new Exception($"Avatar part definition resource not found at path {resourcePath}");
    }
}