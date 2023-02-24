using Network.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

public class AvatarPartRepository : SingletonBehaviour<AvatarPartRepository>
{
    Dictionary<ushort, (HeadShapeGraphicsDefinition, ColorDefinition)> headShapes = new Dictionary<ushort, (HeadShapeGraphicsDefinition, ColorDefinition)>();
    Dictionary<ushort, (HairGraphicsDefinition, ColorDefinition)> hairs = new Dictionary<ushort, (HairGraphicsDefinition, ColorDefinition)>();
    Dictionary<ushort, EyesGraphicsDefinition> eyes = new Dictionary<ushort, EyesGraphicsDefinition>();
    Dictionary<ushort, GlassesGraphicsDefinition> glasses = new Dictionary<ushort, GlassesGraphicsDefinition>();
    Dictionary<ushort, (MouthGraphicsDefinition, ColorDefinition)> mouths = new Dictionary<ushort, (MouthGraphicsDefinition, ColorDefinition)>();

    protected override bool IsGlobal => true;

    public IReadOnlyDictionary<ushort, (HeadShapeGraphicsDefinition graphic, ColorDefinition color)> HeadShapes => headShapes;
    public IReadOnlyDictionary<ushort, (HairGraphicsDefinition graphic, ColorDefinition color)> Hairs => hairs;
    public IReadOnlyDictionary<ushort, EyesGraphicsDefinition> Eyes => eyes;
    public IReadOnlyDictionary<ushort, GlassesGraphicsDefinition> Glasses => glasses;
    public IReadOnlyDictionary<ushort, (MouthGraphicsDefinition graphic, ColorDefinition color)> Mouths => mouths;

    public void Initialize(IEnumerable<AvatarPartConfigDTO> avatarParts)
    {
        var eyesDefinitions = AvatarPartDefinition.Read("Avatar/Definitions/eyes");
        var mouthDefinitions = AvatarPartDefinition.Read("Avatar/Definitions/mouths");
        var glassesDefinitions = AvatarPartDefinition.Read("Avatar/Definitions/glasses");
        var headShapeDefinitions = AvatarPartDefinition.Read("Avatar/Definitions/headshapes");
        var hairDefinitions = AvatarPartDefinition.Read("Avatar/Definitions/hairs");

        var hairColors = new Dictionary<int, ColorDefinition>();
        var skinColors = new Dictionary<int, ColorDefinition>();
        var headShapeGraphics = new Dictionary<int, HeadShapeGraphicsDefinition>();
        var hairGraphics = new Dictionary<int, HairGraphicsDefinition>();
        var mouthGraphics = new Dictionary<int, MouthGraphicsDefinition>();

        var eyesAtlas = Resources.Load<SpriteAtlas>("Avatar/Atlas_Eyes");
        var glassesAtlas = Resources.Load<SpriteAtlas>("Avatar/Atlas_Glasses");
        var hairsAtlas = Resources.Load<SpriteAtlas>("Avatar/Atlas_Hairs");
        var headShapesAtlas = Resources.Load<SpriteAtlas>("Avatar/Atlas_HeadShapes");
        var mouthsAtlas = Resources.Load<SpriteAtlas>("Avatar/Atlas_Mouths");

        foreach (var a in avatarParts)
        {
            switch (a.PartType)
            {
                case AvatarPartType.Eyes:
                    eyes[a.ID] = EyesGraphicsDefinition.Read(eyesDefinitions[a.ID].GraphicsDefinitionID.ToString(), $"Eyes/{eyesDefinitions[a.ID].GraphicsDefinitionID}", eyesAtlas);
                    break;

                case AvatarPartType.Glasses:
                    glasses[a.ID] = GlassesGraphicsDefinition.Read(glassesDefinitions[a.ID].GraphicsDefinitionID.ToString(), $"Glasses/{glassesDefinitions[a.ID].GraphicsDefinitionID}", glassesAtlas);
                    break;

                case AvatarPartType.Hair:
                    {
                        var def = hairDefinitions[a.ID];
                        if (!hairColors.TryGetValue(def.ColorID, out var color))
                            hairColors[def.ColorID] = color = ColorDefinition.Read($"Avatar/Colors/Hair/{def.ColorID}");
                        if (!hairGraphics.TryGetValue(def.GraphicsDefinitionID, out var graphics))
                            hairGraphics[def.ColorID] = graphics = HairGraphicsDefinition.Read(def.GraphicsDefinitionID.ToString(), $"Hairs/{def.GraphicsDefinitionID}", hairsAtlas);
                        hairs[a.ID] = (graphics, color);
                    }
                    break;

                case AvatarPartType.HeadShape:
                    {
                        var def = headShapeDefinitions[a.ID];
                        if (!skinColors.TryGetValue(def.ColorID, out var color))
                            skinColors[def.ColorID] = color = ColorDefinition.Read($"Avatar/Colors/Skin/{def.ColorID}");
                        if (!headShapeGraphics.TryGetValue(def.GraphicsDefinitionID, out var graphics))
                            headShapeGraphics[def.ColorID] = graphics = HeadShapeGraphicsDefinition.Read(def.GraphicsDefinitionID.ToString(), $"HeadShapes/{def.GraphicsDefinitionID}", headShapesAtlas);
                        headShapes[a.ID] = (graphics, color);
                    }
                    break;

                case AvatarPartType.Mouth:
                    {
                        var def = mouthDefinitions[a.ID];
                        if (!hairColors.TryGetValue(def.ColorID, out var color))
                            hairColors[def.ColorID] = color = ColorDefinition.Read($"Avatar/Colors/Hair/{def.ColorID}");
                        if (!mouthGraphics.TryGetValue(def.GraphicsDefinitionID, out var graphics))
                            mouthGraphics[def.ColorID] = graphics = MouthGraphicsDefinition.Read(def.GraphicsDefinitionID.ToString(), $"Mouths/{def.GraphicsDefinitionID}", mouthsAtlas);
                        mouths[a.ID] = (graphics, color);
                    }
                    break;

                default:
                    throw new Exception("Unknown avatar part type " + a.PartType.ToString());
            }
        }
    }

    public IEnumerable<ushort> GetPartIDs(AvatarPartType partType)
    {
        switch (partType)
        {
            case AvatarPartType.Eyes:
                return Eyes.Keys;

            case AvatarPartType.Glasses:
                return Glasses.Keys;

            case AvatarPartType.Hair:
                return Hairs.Keys;

            case AvatarPartType.HeadShape:
                return HeadShapes.Keys;

            case AvatarPartType.Mouth:
                return Mouths.Keys;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
