using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Lens.graphics.animation;

public class ColorMap
{
    private static ushort _count = 0;
    public static ColorMap Empty = new([]);

    private readonly Dictionary<Color, Color> _colors = new();
    public ushort Id { get; }

    public static ColorMap New(IEnumerable<(Color, Color)> colors)
    {
        return new ColorMap(colors);
    }

    private ColorMap(IEnumerable<(Color, Color)> colors)
    {
        Id = _count;
        _count += 1;
        foreach (var (k, v) in colors)
        {
            _colors[k] = v;
        }
    }

    internal bool IsEmpty1()
    {
        return _colors.Count == 0;
    }

    public void ReColor(Color[] colorArray)
    {
        for (var i = 0; i < colorArray.Length; ++i)
        {
            if (_colors.TryGetValue(colorArray[i], out var newColor))
            {
                colorArray[i] = newColor;
            }
        }
    }
}

public static class ColorMapExt
{
    public static bool IsEmpty(this ColorMap colorMap)
    {
        return colorMap == null || colorMap.IsEmpty1();
    }
}