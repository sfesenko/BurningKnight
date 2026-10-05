using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lens.util;

/// <summary>The engine's logging facade: one console line plus — in Release — one line in
/// <c>burning_log.txt</c>. MonoGame ships no runtime logging API, so the engine provides this one.
/// Writes are serialised and flushed (a crash keeps the tail); every line carries its call site.
/// <see cref="Debug"/> and <see cref="Assert"/> are compiled out of Release entirely.
/// The implementation is private on purpose — this type is the seam.</summary>
public static class Log {
	private static string LogName => Path.Combine(Paths.DataDir, "burning_log.txt");
	private static string PrevLogName => Path.Combine(Paths.DataDir, "burning_log.prev.txt");

	public static readonly bool WriteToFile = !Engine.Debug;

	private static readonly object Lock = new();

	// Android has no console (the setter throws): probe once so one facade serves both.
	private static readonly bool Colors = ProbeColors();

	private static bool ProbeColors() {
		try {
			// Probe the setter too: a write throwing mid-line is worse than no colors.
			var old = Console.ForegroundColor;
			Console.ForegroundColor = old;

			return true;
		} catch (Exception) {
			// Any console failure means no colors; a static-init throw kills the process pre-frame.
			return false;
		}
	}
	private static StreamWriter? writer;

	public static void Open() {
		lock (Lock) {
			try {
				// Keep one previous log across a relaunch: the last run's tail is the bug report.
				// Best effort — a failed roll costs history, not the log.
				if (File.Exists(PrevLogName)) {
					File.Delete(PrevLogName);
				}

				if (File.Exists(LogName)) {
					File.Move(LogName, PrevLogName);
				}
			} catch (Exception e) {
				// A failed roll costs history, not the log (append mode below).
				try {
					Console.Error.WriteLine(e);
				} catch {
					// Stdout may be closed; logging must never take the process down.
				}
			}

			if (WriteToFile) {
				// Close the previous writer first (locale-change reopen): a stale one keeps its
				// old file position and would overwrite newer content mid-file after the roll.
				Close();

				// AutoFlush: every line reaches disk as written, so a crash keeps the tail.
				writer = new StreamWriter(new FileStream(LogName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) {
					AutoFlush = true
				};
			}
		}
	}

	public static void Flush() {
		lock (Lock) {
			try {
				writer?.Flush();
			} catch {
				// Nowhere left to report to.
			}
		}
	}

	public static void Close() {
		lock (Lock) {
			var w = writer;
			writer = null;

			try {
				w?.Close();
			} catch (Exception e) {
				Error(e);
			}
		}
	}

	/// <summary>
	/// Logs an error with a stack trace when <paramref name="condition"/> is false.
	/// Compiled out of Release builds entirely.
	/// </summary>
	[Conditional("DEBUG")]
	public static void Assert(bool condition, Func<string>? message = null,
		[CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) {
		if (!condition) {
			var msg = message?.Invoke() ?? "";
			msg += new StackTrace(true);

			Print(msg, ConsoleColor.DarkRed, "ERR", file, member, line);
		}
	}

	public static void Info(object message,
		[CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) {
		Print(message, ConsoleColor.Green, "INF", file, member, line);
	}

	[Conditional("DEBUG")]
	public static void Debug(object message,
		[CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) {
		Print(message, ConsoleColor.Blue, "DBG", file, member, line);
	}

	public static void Error(object message,
		[CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) {
		Print(message, ConsoleColor.DarkRed, "ERR", file, member, line);
	}

	public static void Warning(object message,
		[CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0) {
		Print(message, ConsoleColor.DarkYellow, "WRN", file, member, line);
	}

	private static void Print(object message, ConsoleColor color, string type, string file, string member, int line) {
		var time = $"{DateTime.Now:HH:mm:ss}";
		var caller = GetCaller(file, member, line);

		// Each line is built once, then written once under the lock: no interleaving, no
		// half-lines from another thread.
		var fileLine = $"{time}| {type}| {message} {caller}";
		var consoleLine = $"{time} {type} {message} {caller}";

		lock (Lock) {
			// A logging facade must never take the process down: disk-full,
			// unmounted FilesDir, or closed stdout all surface here.
			try {
				if (writer != null) {
					writer.WriteLine(fileLine);
				}

				if (Colors) {
					var old = Console.ForegroundColor;

					Console.ForegroundColor = ConsoleColor.Gray;
					Console.Write(time);
					Console.Write(' ');
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write(type);
					Console.Write(' ');
					Console.ForegroundColor = color;
					Console.Write(message);
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.Write(' ');
					Console.WriteLine(caller);

					Console.ForegroundColor = old;
				} else {
					Console.WriteLine(consoleLine);
				}
			} catch {
				// Nowhere left to report to.
			}
		}
	}

	private static string GetCaller(string file, string member, int line) {
		var caller = $"{Path.GetFileName(file)}:{member}():{line}";

#if DEBUG
		// The frame above the call site, so one line tells you both where it was logged and
		// from where. Only in DEBUG: a stack walk per line is not worth it in Release.
		var frame = new StackTrace(true).GetFrame(4);

		if (frame != null) {
			caller += $" <= {Path.GetFileName(frame.GetFileName())}:{frame.GetMethod()?.Name}():{frame.GetFileLineNumber()}";
		}
#endif

		return caller;
	}
}
