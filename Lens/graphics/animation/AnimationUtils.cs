using System.Collections.Generic;
using Aseprite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lens.graphics.animation;

public static class AnimationUtils
{
    /// <summary>
    /// Load .ase animation
    /// </summary>
    /// <param name="fileName"></param>
    public static AnimationData LoadAnimation(string fileName)
    {
        var file = AsepriteFile.ReadAsepriteFile(fileName);
        var texture = new Texture2D(Engine.GraphicsDevice, file.TextureWidth, file.TextureHeight + 1);
        texture.SetData(file.PixelData);
        
        var animation = new AnimationData
        {
            Texture = texture
        };

        for (var i = 0; i < file.Layers.Count; i++)
        {
            var layer = file.Layers[i];
            var list = new List<AnimationFrame>();

            for (var j = 0; j < file.Frames.Count; j++)
            {
                var frame = file.Frames[j];
                var newFrame = new AnimationFrame
                {
                    Duration = frame.Duration,
                    Texture = new TextureRegion(texture,
                        new Rectangle(j * file.Width, i * file.Height, file.Width, file.Height))
                };

                newFrame.Bounds = newFrame.Texture.Source;

                list.Add(newFrame);
            }

            animation.Layers[layer.Name] = list;
        }

        foreach (var slice in file.Slices)
        {
            animation.Slices[slice.Name] = new TextureRegion(texture,
                new Rectangle(slice.OriginX, slice.OriginY, slice.Width, slice.Height));
        }

        foreach (var tag in file.Animations.Values)
        {
            var newTag = new AnimationTag
            {
                Direction = (AnimationDirection)tag.Directions,
                StartFrame = (uint)tag.FirstFrame,
                EndFrame = (uint)tag.LastFrame
            };

            animation.Tags[tag.Name] = newTag;
        }

        foreach (var tag in file.Tags)
        {
            var newTag = new AnimationTag
            {
                Direction = (AnimationDirection)tag.LoopDirection,
                StartFrame = (uint)tag.From,
                EndFrame = (uint)tag.To
            };

            animation.Tags[tag.Name] = newTag;
        }

        return animation;
    }
}