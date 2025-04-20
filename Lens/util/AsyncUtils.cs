using System;
using System.Threading.Tasks;

namespace Lens.util;

public static class AsyncUtils
{
    public static async Task RunAsync(string name, Action action)
    {
        var start = DateTime.Now;
        var task = Task.Run(action);
        await task;
        var end = DateTime.Now;
        var time = end - start;
        Log.Debug($"run: {name}, {time.TotalMilliseconds}ms");
    }

    public static void RunSync(string name, Action action)
    {
        var t = RunAsync(name, action);
        Task.WaitAll(t);
    }
}
