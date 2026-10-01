using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lens.util;

/// <summary>
/// The engine's logging facade: one coloured console line, and — in Release — one line in
/// <c>burning_log.txt</c> beside the game's other data. MonoGame ships no runtime logging API,
/// so the engine provides this one.
///
/// Writes are serialised and flushed, so threads cannot interleave or lose lines and a crash
/// keeps the tail. Every line carries its call site; in DEBUG it also carries the frame above
/// it. <see cref="Debug"/> and <see cref="Assert"/> are compiled out of Release builds
/// entirely, so their messages cost nothing there.
///
/// The implementation is private on purpose — this type is the seam, and no call site can tell
/// what sits behind it.
/// </summary>
public static class Log {
	private static string LogName => Path.Combine(Paths.DataDir, "burning_log.txt");

	public static readonly bool WriteToFile = !Engine.Debug;

	private static readonly object Lock = new();
	private static StreamWriter? writer;

	public static void Open() {
		lock (Lock) {
			if (File.Exists(LogName)) {
				try {
					File.Delete(LogName);
				} catch (Exception e) {
					// The file is only opened in append mode, so a failed delete costs history,
					// not the log.
					Console.Error.WriteLine(e);
				}
			}

			if (WriteToFile) {
				// AutoFlush puts every line on disk as it is written, so the tail survives a
				// crash — which is what the file is for.
				writer = new StreamWriter(new FileStream(LogName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) {
					AutoFlush = true
				};
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

		lock (Lock) {
			writer?.Write(time);
			writer?.Write("| ");
			writer?.Write(type);
			writer?.Write("| ");
			writer?.Write(message);
			writer?.Write(' ');
			writer?.WriteLine(caller);

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
