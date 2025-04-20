using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace Lens.util;
public static class Log {
	private const string LogName = "burning_log.txt";

	public static bool WriteToFile = !Engine.Debug;
	private static Vector2 size = new(300, 400);
	private static StreamWriter? writer;

	public static void Open() {
		if (File.Exists(LogName)) {
			try {
				File.Delete(LogName);
			} catch (Exception e) {

			}
		}

		if (WriteToFile) {
			writer = new StreamWriter(new FileStream(LogName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
		}
	}

	public static void Close() {
		try {
			writer?.Close();
			writer = null;
		} catch (Exception e) {
			Error(e);
		}
	}

	public static void Assert(bool condiftion, Func<string>? message = null)
	{
#if DEBUG
		var msg = message?.Invoke() ?? "";
		var st = new StackTrace(true);
		msg += st;
		Error(msg);
#endif
	}
	
	public static void Info(object message) {
		Print(message, ConsoleColor.Green, "INF");
	}
	
	public static void Debug(object message) {
		Print(message, ConsoleColor.Blue, "DBG");
	}
	
	public static void Error(object message) {
		Print(message, ConsoleColor.DarkRed, "ERR");
	}
	
	public static void Warning(object message) {
		Print(message, ConsoleColor.DarkYellow, "WRN");
	}

	private static void Print(object message, ConsoleColor color, string type)
	{
		var color1 = Print1(message, color, type);
#if DEBUG
		PrintStack();	
#else
		writer?.WriteLine();
		Console.WriteLine();
#endif
		Console.ForegroundColor = color1;
	}

	private static ConsoleColor Print1(object message, ConsoleColor color, string type)
	{
		var time = $"{DateTime.Now:HH:mm:ss}";
		writer?.Write(time);
		writer?.Write("| ");
		writer?.Write(type);
		writer?.Write("| ");
		writer?.Write(message);
		//
		var oldColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Gray;
		Console.Write(time);
		Console.Write(' ');
		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.Write(type);
		Console.Write(' ');
		Console.ForegroundColor = color;
		Console.Write(message);

		return oldColor;
	}
	
	private static void PrintStack()
	{
		var stackTrace = new StackTrace(true);
		var frame = stackTrace.GetFrame(3);
		var prev = stackTrace.GetFrame(4);

		var frameFileName = frame?.GetFileName();
		var frameMethodName = frame?.GetMethod()?.Name;
		var frameLineNumber = frame?.GetFileLineNumber();

		var text = prev == null 
			? $"{Path.GetFileName(frameFileName)}:{frameMethodName}():{frameLineNumber}" 
			: $"{Path.GetFileName(frameFileName)}:{frameMethodName}():{frameLineNumber} <= {Path.GetFileName(prev.GetFileName())}:{prev?.GetMethod()?.Name}():{prev?.GetFileLineNumber()} ";
		
		writer?.Write(' ');
		writer?.WriteLine(text);
		
		Console.ForegroundColor = ConsoleColor.Gray;
		Console.Write(' ');
		Console.WriteLine(text);
	}
}
