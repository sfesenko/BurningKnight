using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.graphics.animation;

public static class AnimationUtils
{
    /// <summary>
    /// Reads one entry of the preprocessor's animations.json into plain data. The document it came
    /// from may be disposed as soon as this returns, so nothing here keeps a JsonElement alive.
    /// </summary>
    public static AnimationSource ReadSource(JsonElement entry)
    {
        var frames = entry.GetProperty("frames").GetInt32();
        var durations = new float[frames];
        var i = 0;

        foreach (var duration in entry.GetProperty("durations").EnumerateArray())
        {
            durations[i++] = duration.GetSingle();
        }

        var layers = new List<string>();

        foreach (var layer in entry.GetProperty("layers").EnumerateArray())
        {
            layers.Add(layer.GetString());
        }

        var slices = new List<(string, Rectangle)>();

        foreach (var slice in entry.GetProperty("slices").EnumerateObject())
        {
            slices.Add((slice.Name, new Rectangle(
                slice.Value.GetProperty("x").GetInt32(),
                slice.Value.GetProperty("y").GetInt32(),
                slice.Value.GetProperty("width").GetInt32(),
                slice.Value.GetProperty("height").GetInt32())));
        }

        var tags = new List<(string, uint, uint, int)>();

        foreach (var tag in entry.GetProperty("tags").EnumerateObject())
        {
            tags.Add((tag.Name,
                (uint) tag.Value.GetProperty("from").GetInt32(),
                (uint) tag.Value.GetProperty("to").GetInt32(),
                tag.Value.GetProperty("direction").GetInt32()));
        }

        return new AnimationSource
        {
            Width = entry.GetProperty("width").GetInt32(),
            Height = entry.GetProperty("height").GetInt32(),
            Frames = frames,
            Durations = durations,
            Layers = layers.ToArray(),
            Slices = slices.ToArray(),
            Tags = tags.ToArray()
        };
    }

    /// <summary>
    /// Builds an animation from a source and the sheet it was cut from. Layer order is the file's,
    /// and each layer is a horizontal band: frame i of layer j is (i * width, j * height, width,
    /// height).
    /// </summary>
    public static AnimationData LoadAnimation(Texture2D texture, AnimationSource source)
    {
        var animation = new AnimationData
        {
            Texture = texture
        };

        var band = 0;

        foreach (var layer in source.Layers)
        {
            var list = new List<AnimationFrame>(source.Frames);

            for (var i = 0; i < source.Frames; i++)
            {
                var frame = new AnimationFrame
                {
                    Duration = source.Durations[i],
                    Texture = new TextureRegion(texture,
                        new Rectangle(i * source.Width, band * source.Height, source.Width, source.Height))
                };

                frame.Bounds = frame.Texture.Source;

                list.Add(frame);
            }

            animation.Layers[layer] = list;
            band++;
        }

        foreach (var (name, bounds) in source.Slices)
        {
            animation.Slices[name] = new TextureRegion(texture, bounds);
        }

        foreach (var (name, from, to, direction) in source.Tags)
        {
            animation.Tags[name] = new AnimationTag
            {
                Direction = (AnimationDirection) direction,
                StartFrame = from,
                EndFrame = to
            };
        }

        return animation;
    }
}
