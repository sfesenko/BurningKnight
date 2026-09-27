using System;
using System.Threading.Tasks;

namespace Lens.util;

public static class AsyncUtils
{
    public static async Task RunAsync(string name, Action action)
    {
        var start = DateTime.Now;

        try {
            await Task.Run(action);
        } catch (Exception e) {
            // Most callers deliberately do not await this, so an escaping exception would be
            // stored in an unobserved task and never surface. Log it instead of losing it.
            Log.Error(e);
        }

        var time = DateTime.Now - start;
        Log.Debug($"run: {name}, {time.TotalMilliseconds}ms");
    }

    public static void RunSync(string name, Action action)
    {
        var t = RunAsync(name, action);
        Task.WaitAll(t);
    }
}
