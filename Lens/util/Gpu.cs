using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Lens.util;

/// <summary>The main thread's GPU queue: MonoGame's <c>GraphicsDevice</c> is not documented as
/// thread-safe, so every texture/shader/render-target creation goes through here — workers defer
/// and wait while the main thread drains a few ms per frame, keeping the loading screen alive.
/// On the main thread everything runs immediately (tests, editors), so call sites never ask which
/// thread they are on. <see cref="Flush"/> must run once per frame on the main thread.</summary>
public static class Gpu {
	private const double FrameBudgetMs = 8;

	private static int mainThreadId;

	private static readonly object Gate = new();
	private static readonly Queue<Action> Queue = new();
	private static readonly ManualResetEventSlim Drained = new(true);
	private static readonly Stopwatch Budget = new();

	/// <summary>The host calls this from the thread that owns the device, before any load.</summary>
	public static void MarkMainThread() {
		mainThreadId = Environment.CurrentManagedThreadId;
	}

	// Before the host marks the thread (a headless tool, a test that loads before the engine
	// exists) there is no worker to defer to, so treat everything as the main thread.
	public static bool IsMainThread => mainThreadId == 0 || Environment.CurrentManagedThreadId == mainThreadId;

	/// <summary>Runs the action on the main thread; a worker blocks until it has run.</summary>
	/// <remarks>Deferred actions must not throw: Flush runs them on the main thread, past the
	/// queuing caller's try/catch. Catch inside the closure.</remarks>
	public static void Run(Action action) {
		if (IsMainThread) {
			action();
			return;
		}

		Defer(action);
		Wait();
	}

	/// <summary>Runs the function on the main thread and returns its result.</summary>
	/// <remarks>Same no-throw contract as <see cref="Run(Action)"/>: catch inside the closure.</remarks>
	public static T Run<T>(Func<T> action) {
		if (IsMainThread) {
			return action();
		}

		T result = default!;

		Defer(() => result = action());
		Wait();

		return result;
	}

	/// <summary>Queues the action for the main thread; on the main thread it runs now.</summary>
	/// <remarks>Same no-throw contract as <see cref="Run(Action)"/>: catch inside the closure.</remarks>
	public static void Defer(Action action) {
		if (IsMainThread) {
			action();
			return;
		}

		lock (Gate) {
			Queue.Enqueue(action);
			Drained.Reset();
		}
	}

	/// <summary>Waits for the queue to drain; a no-op on the main thread.</summary>
	public static void Wait() {
		if (IsMainThread) {
			return;
		}

		Drained.Wait();
	}

	/// <summary>The main thread runs the queue, a few milliseconds per call.</summary>
	/// <remarks>Belt-and-suspenders behind the no-throw contract: a throwing closure is logged
	/// and skipped instead of killing the frame loop and hanging <see cref="Wait"/> forever.</remarks>
	public static void Flush() {
		if (!IsMainThread) {
			return;
		}

		Budget.Restart();

		while (true) {
			Action action;

			lock (Gate) {
				if (Queue.Count == 0) {
					Drained.Set();
					return;
				}

				action = Queue.Dequeue();
			}

			try {
				action();
			} catch (Exception e) {
				Log.Error(e);
			}

			if (Budget.Elapsed.TotalMilliseconds >= FrameBudgetMs) {
				return;
			}
		}
	}
}
