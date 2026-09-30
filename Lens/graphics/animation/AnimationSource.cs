#nullable enable

using Microsoft.Xna.Framework;

namespace Lens.graphics.animation;

/// <summary>
/// One animation as the preprocessor's animations.json describes it: frame size, per-frame
/// durations, layer order, slices and tags. It holds no JSON state, so a reload may replace it
/// while a sheet is still being built from it.
/// </summary>
public sealed class AnimationSource
{
    public int Width;
    public int Height;
    public int Frames;
    // All four are set by the loader's object initializer.
    public float[] Durations = null!;
    public string[] Layers = null!;
    public (string Name, Rectangle Bounds)[] Slices = null!;
    public (string Name, uint From, uint To, int Direction)[] Tags = null!;
}
