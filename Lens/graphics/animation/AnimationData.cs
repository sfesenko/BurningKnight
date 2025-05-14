using System.Collections.Generic;
using System.Linq;
using Lens.assets;
using Lens.util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.graphics.animation;

public struct AnimationTag {
    public uint StartFrame;
    public uint EndFrame;
    public AnimationDirection Direction;
}

public struct AnimationFrame {
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

    public AnimationData Recolor(ColorSet set)
    {
        var newAnimation = new AnimationData();
        var w = Texture.Width;
        var h = Texture.Height;
        var texture = new Texture2D(Engine.GraphicsDevice, w, h);
        var tdata = new Color[w * h];
			
        Texture.GetData(tdata);
        var pixelData = new Color[w * h];
			
        for (var y = 0; y < h; y++) {
            for (var x = 0; x < w; x++) {
                var i = x + y * w;
                var color = tdata[i];

                for (var c = 0; c < set.From.Length; c++) {
                    if (ColorUtils.Compare(set.From[c], color, 4)) {
                        color = set.To[c];
                    }
                }
					
                pixelData[i] = color;
            }
        }
			
        texture.SetData(pixelData);

        foreach (var l in this.Layers)
        {
            newAnimation.Layers[l.Key] =
                l.Value.Select(f => f with { Texture = new TextureRegion(texture, f.Bounds) })
                    .ToList();
        }

        foreach (var (key, value) in this.Slices) {
            newAnimation.Slices[key] = new TextureRegion(texture, value.Source);
        }

        foreach (var t in this.Tags) {
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