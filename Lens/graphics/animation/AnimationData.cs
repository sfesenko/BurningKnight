using System.Collections.Generic;
using System.Linq;
using Lens.assets;
using Lens.util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.graphics.animation;

public struct AnimationTag
{
    public uint StartFrame;
    public uint EndFrame;
    public AnimationDirection Direction;
}

public struct AnimationFrame
{
    public float Duration;
    public Rectangle Bounds;
    public TextureRegion Texture;
}

public class AnimationData
{
    public readonly Dictionary<string, List<AnimationFrame>> Layers = new();
    public readonly Dictionary<string, AnimationTag> Tags = new();
    public readonly Dictionary<string, TextureRegion> Slices = new();
    public Texture2D Texture;

    public AnimationData Recolor(ColorMap colorMap)
    {
        var newAnimation = new AnimationData();
        var colorData = new Color[Texture.Width * Texture.Height];
        Texture.GetData(colorData);

        colorMap.ReColor(colorData);

        var texture = new Texture2D(Engine.GraphicsDevice, Texture.Width, Texture.Height);
        texture.SetData(colorData);

        foreach (var l in Layers)
        {
            newAnimation.Layers[l.Key] =
                l.Value.Select(f => f with { Texture = new TextureRegion(texture, f.Bounds) })
                    .ToList();
        }

        foreach (var (key, value) in Slices)
        {
            newAnimation.Slices[key] = new TextureRegion(texture, value.Source);
        }

        foreach (var t in Tags)
        {
            newAnimation.Tags[t.Key] = t.Value;
        }

        newAnimation.Texture = texture;

        return newAnimation;
    }

    public AnimationTag? GetTag(string tagName)
    {
        AnimationTag tag;

        if (tagName == null)
        {
            tag = Tags.FirstOrDefault().Value;
        }
        else if (!Tags.TryGetValue(tagName, out tag))
        {
            return null;
        }

        return tag;
    }

    public TextureRegion GetSlice(string name, bool error = true)
    {
        if (Slices.TryGetValue(name, out var region))
        {
            return region;
        }

        if (!error)
        {
            return null;
        }

        Log.Warning($"Unable to find slice {name}");
        return Textures.Missing;
    }

    public AnimationFrame? GetFrame(string layer, uint id)
    {
        List<AnimationFrame> frames;

        if (layer == null)
        {
            frames = Layers.FirstOrDefault().Value;
        }
        else if (!Layers.TryGetValue(layer, out frames))
        {
            return null;
        }

        if (frames.Count < id)
        {
            Log.Warning($"Unable to find frame {layer}:{id}");
            return null;
        }

        return frames[(int)id];
    }

    public Animation CreateAnimation(string layer = null)
    {
        return new Animation(this, layer);
    }
}