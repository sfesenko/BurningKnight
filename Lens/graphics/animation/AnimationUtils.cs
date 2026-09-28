using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.graphics.animation;

public static class AnimationUtils
{
    /// <summary>
    /// Builds an animation from one entry of the preprocessor's animations.json and the sheet it
    /// was cut from. Layer order is the file's, and each layer is a horizontal band: frame i of
    /// layer j is the rectangle (i * width, j * height, width, height).
    /// </summary>
    public static AnimationData LoadAnimation(Texture2D texture, JsonElement entry)
    {
        var width = entry.GetProperty("width").GetInt32();
        var height = entry.GetProperty("height").GetInt32();
        var frames = entry.GetProperty("frames").GetInt32();
        var durations = entry.GetProperty("durations");

        var animation = new AnimationData
        {
            Texture = texture
        };

        var band = 0;

        foreach (var layer in entry.GetProperty("layers").EnumerateArray())
        {
            var list = new List<AnimationFrame>(frames);

            for (var i = 0; i < frames; i++)
            {
                var frame = new AnimationFrame
                {
                    Duration = durations[i].GetSingle(),
                    Texture = new TextureRegion(texture,
                        new Rectangle(i * width, band * height, width, height))
                };

                frame.Bounds = frame.Texture.Source;

                list.Add(frame);
            }

            animation.Layers[layer.GetString()] = list;
            band++;
        }

        foreach (var slice in entry.GetProperty("slices").EnumerateObject())
        {
            var bounds = new Rectangle(
                slice.Value.GetProperty("x").GetInt32(),
                slice.Value.GetProperty("y").GetInt32(),
                slice.Value.GetProperty("width").GetInt32(),
                slice.Value.GetProperty("height").GetInt32());

            animation.Slices[slice.Name] = new TextureRegion(texture, bounds);
        }

        foreach (var tag in entry.GetProperty("tags").EnumerateObject())
        {
            animation.Tags[tag.Name] = new AnimationTag
            {
                Direction = (AnimationDirection) tag.Value.GetProperty("direction").GetInt32(),
                StartFrame = (uint) tag.Value.GetProperty("from").GetInt32(),
                EndFrame = (uint) tag.Value.GetProperty("to").GetInt32()
            };
        }

        return animation;
    }
}
