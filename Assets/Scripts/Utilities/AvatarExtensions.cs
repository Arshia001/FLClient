using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Network.Types;
using UnityEngine;

public static class AvatarExtensions
{
    public static ushort? GetPart(this AvatarDTO avatar, AvatarPartType type) => 
        avatar.Parts.FirstOrDefault(p => p.PartType == type)?.ID;

    public static AvatarDTO RemovePart(this AvatarDTO avatar, AvatarPartType part) => 
        new AvatarDTO(avatar.Parts.Where(p => p.PartType != part));

    public static AvatarDTO ReplacePart(this AvatarDTO avatar, AvatarPartDTO part) => 
        new AvatarDTO(avatar.Parts.Where(p => p.PartType != part.PartType).Append(part));

    public static bool IsPartActive(this AvatarDTO avatar, AvatarPartDTO part) =>
        avatar.Parts.Any(p => p.ID == part.ID && p.PartType == part.PartType);

    public static bool IsEquivalentTo(this AvatarDTO avatar, AvatarDTO other) =>
        avatar.Parts.Count == other.Parts.Count && avatar.Parts.All(p => other.Parts.Any(p2 => p.PartType == p2.PartType && p.ID == p2.ID));
}
