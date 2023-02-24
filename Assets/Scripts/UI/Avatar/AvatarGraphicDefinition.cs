using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.U2D;

public enum AvatarColorMap
{
    None,
    Skin,
    Hair,
    Beard
}

public abstract class AvatarGraphicDefinition
{
    protected AvatarGraphicDefinition(Sprite sprite, AvatarColorMap colorMap)
    {
        Sprite = sprite;
        ColorMap = colorMap;
    }

    public AvatarColorMap ColorMap { get; }

    public Sprite Sprite { get; }

    protected static T Read<T>(string name, string resourceSubPath, SpriteAtlas atlas, Func<Sprite, AvatarColorMap, Dictionary<string, string>, T> readConfig)
    {
        var sprite = atlas.GetSprite(name);
        if (sprite == null)
            throw new Exception("Sprite not found in atlas for avatar graphic at resource sub path " + resourceSubPath);

        var textAsset = Resources.Load<TextAsset>($"Avatar/GraphicsDefinitions/{resourceSubPath}");

        if (textAsset != null)
        {
            var config = ResourceConfigReader.Read(textAsset.text);

            var color = AvatarColorMap.None;
            foreach (var kv in config)
                if (kv.Key == "color")
                    if (!Enum.TryParse(kv.Value, true, out color))
                        throw new Exception("Invalid color setting: " + kv.Value);

            var result = readConfig(sprite, color, config);
            Resources.UnloadAsset(textAsset);
            return result;
        }
        else
            return readConfig(sprite, AvatarColorMap.None, new Dictionary<string, string>());
    }
}

public class EyesGraphicsDefinition : AvatarGraphicDefinition
{
    private EyesGraphicsDefinition(Sprite sprite, AvatarColorMap colorMap) : base(sprite, colorMap) { }

    public static EyesGraphicsDefinition Read(string name, string resourceSubPath, SpriteAtlas atlas) =>
        Read(name, resourceSubPath, atlas, (s, color, dic) => new EyesGraphicsDefinition(s, color));
}

public class GlassesGraphicsDefinition : AvatarGraphicDefinition
{
    private GlassesGraphicsDefinition(Sprite sprite, AvatarColorMap colorMap) : base(sprite, colorMap) { }

    public static GlassesGraphicsDefinition Read(string name, string resourceSubPath, SpriteAtlas atlas) =>
        Read(name, resourceSubPath, atlas, (s, color, dic) => new GlassesGraphicsDefinition(s, color));
}

public class HairGraphicsDefinition : AvatarGraphicDefinition
{
    private HairGraphicsDefinition(Sprite sprite, Sprite backSprite, Sprite decoSprite, Sprite backDecoSprite) : base(sprite, AvatarColorMap.Hair)
    {
        BackSprite = backSprite;
        DecoSprite = decoSprite;
        BackDecoSprite = backDecoSprite;
    }

    public Sprite BackSprite { get; }
    public Sprite DecoSprite { get; }
    public Sprite BackDecoSprite { get; }

    public static HairGraphicsDefinition Read(string name, string resourceSubPath, SpriteAtlas atlas) =>
        Read(name, resourceSubPath, atlas, (s, color, dic) =>
        {
            if (color != AvatarColorMap.None)
                throw new Exception("Color map not supported for hair");

            var decoSprite = atlas.GetSprite(name + "d");
            var backSprite = atlas.GetSprite(name + "b");
            var backDecoSprite = atlas.GetSprite(name + "bd");

            return new HairGraphicsDefinition(s, backSprite, decoSprite, backDecoSprite);
        });
}

public class HeadShapeGraphicsDefinition : AvatarGraphicDefinition
{
    private HeadShapeGraphicsDefinition(Sprite sprite, string name) : base(sprite, AvatarColorMap.Skin) => Name = name;

    public string Name { get; }

    public static HeadShapeGraphicsDefinition Read(string name, string resourceSubPath, SpriteAtlas atlas) =>
        Read(name, resourceSubPath, atlas, (sprite, color, dic) =>
        {
            if (color != AvatarColorMap.None)
                throw new Exception("Color map not supported for head shape");

            return new HeadShapeGraphicsDefinition(sprite, name);
        });
}

public class MouthGraphicsDefinition : AvatarGraphicDefinition
{
    private MouthGraphicsDefinition(Sprite sprite, AvatarColorMap color) : base(sprite, color) { }

    public static MouthGraphicsDefinition Read(string name, string resourceSubPath, SpriteAtlas atlas) =>
        Read(name, resourceSubPath, atlas, (s, color, dic) => new MouthGraphicsDefinition(s, color));
}

public class ColorDefinition
{
    static readonly ColorDefinition white = new ColorDefinition(Color.white);

    private ColorDefinition(Color color) => Color = color;

    public Color Color { get; }

    public static ColorDefinition Read(string resourcePath)
    {
        var go = Resources.Load<GameObject>(resourcePath);
        if (go == null && resourcePath.EndsWith("/0"))
            return white;
        var color = go.GetComponent<AvatarPartColor>().Color;
        color.a = 1.0f;
        return new ColorDefinition(color);
    }
}
