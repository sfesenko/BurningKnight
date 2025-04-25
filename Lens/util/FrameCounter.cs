using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Lens.util;

public class FrameCounter
{
    private const int MaximumSamples = 30;

    private readonly Queue<int> _buffer = new();

    public int AverageFramesPerSecond { get; private set; }
    public int CurrentFramesPerSecond { get; private set; }

    public void Update(GameTime gt)
    {
        CurrentFramesPerSecond = (int)Math.Round(1f / gt.ElapsedGameTime.TotalSeconds);

        _buffer.Enqueue(CurrentFramesPerSecond);

        if (_buffer.Count > MaximumSamples)
        {
            _buffer.Dequeue();
        }

        AverageFramesPerSecond = (int)Math.Round(_buffer.Average(i => i));
    }
}