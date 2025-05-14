using System;
using System.Collections.Generic;

namespace Lens.util.timer;

public class TimerTask(Action fn)
{
    public readonly Action? Fn = fn;

    public void Cancel()
    {
        fn = null;
    }
}

public static class Timer
{
    private static readonly PriorityQueue<TimerTask, float> Tasks = new();
    private static float _time;

    public static TimerTask Add(Action fn, float delay)
    {
        if (delay <= 0)
        {
            fn();
            return null;
        }

        var t = new TimerTask(fn);
        Tasks.Enqueue(t, _time + delay);
        return t;
    }

    public static void Clear()
    {
        Tasks.Clear();
    }

    public static void Update(float dt)
    {
        _time += dt;

        while (Tasks.TryPeek(out _, out var time) && _time >= time)
        {
            var t = Tasks.Dequeue();
            t.Fn?.Invoke();
        }
    }
}