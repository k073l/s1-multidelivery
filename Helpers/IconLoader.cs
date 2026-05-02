using System.Reflection;
using S1API.Utils;
using UnityEngine;

namespace MultiDelivery.Helpers;

public class IconLoader
{
    public static Sprite QuestIconSprite =>
        GetIcon(ref _questIconSprite, $"{nameof(MultiDelivery)}.assets.quest_icon.png");

    private static Sprite _questIconSprite;


    private static Sprite LoadEmbeddedPNG(string resourceName)
    {
        var asm = Assembly.GetExecutingAssembly();

        using var stream = asm.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, data.Length);
        var sprite = ImageUtils.LoadImageRaw(data);
        if (sprite != null) sprite.name = resourceName;
        return sprite;
    }

    private static Sprite GetIcon(ref Sprite spriteField, string resourceName)
    {
        if (spriteField == null)
        {
            spriteField = LoadEmbeddedPNG(resourceName);
        }

        return spriteField;
    }
}