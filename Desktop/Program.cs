using System;
using System.Diagnostics;
using System.IO;
using Desktop.integration.crash;
using Lens.assets;
using Microsoft.Xna.Framework.Audio;

namespace Desktop {
	public class Program {
		// The game records its own pid here so the next launch can stop it. A user-level path
		// rather than one relative to the working directory, so it is the same file whichever
		// build output the game was started from.
		private static readonly string InstanceFile = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burning_knight", "instance.pid");

		private static void TryToRemove(string file) {
			if (File.Exists(file)) {
				try {
					File.Delete(file);
				} catch (Exception e) {
					Console.WriteLine(e);
				}
			}
		}
		
		private static void RemoveOwnInstance() {
			try {
				if (File.Exists(InstanceFile) && File.ReadAllText(InstanceFile).StartsWith($"{Environment.ProcessId} ")) {
					File.Delete(InstanceFile);
				}
			} catch (Exception e) {
				Console.WriteLine(e);
			}
		}
		
		// Process.GetProcessesByName matches on the process name alone, and a framework-dependent
		// launch shares the name "dotnet" with every other .NET process on the machine — builds,
		// MSBuild nodes, language servers. Identify the previous instance by the pid it recorded
		// instead, and check the start time so a recycled pid is not killed by mistake.
		private static void KillPreviousInstance() {
			var current = Process.GetCurrentProcess();

			try {
				var record = File.Exists(InstanceFile) ? File.ReadAllText(InstanceFile).Split(' ') : null;

				if (record is { Length: 2 }
				    && int.TryParse(record[0], out var pid)
				    && long.TryParse(record[1], out var startTicks)) {
					try {
						var previous = Process.GetProcessById(pid);

						// On Unix StartTime is derived from /proc/uptime, which is quantised to
						// centiseconds, so two reads of the same process can differ by a few
						// milliseconds. Compare with a tolerance wide enough to cover that and far
						// narrower than a pid could plausibly be recycled within.
						var drift = Math.Abs(previous.StartTime.Ticks - startTicks);

						if (drift < TimeSpan.TicksPerSecond) {
							previous.CloseMainWindow();
							previous.Kill();
							Console.WriteLine($"Killing the previous instance (pid {pid})");
						}
					} catch (ArgumentException) {
						// The pid is gone; the file is just stale.
					} catch (Exception e) {
						Console.WriteLine($"Failed to stop the previous instance: {e.Message}");
					}
				}
			} catch (Exception e) {
				Console.WriteLine($"Failed to read the instance file: {e.Message}");
			}

			try {
				Directory.CreateDirectory(Path.GetDirectoryName(InstanceFile));
				File.WriteAllText(InstanceFile, $"{current.Id} {current.StartTime.Ticks}");
			} catch (Exception e) {
				Console.WriteLine($"Failed to write the instance file: {e.Message}");
			}
		}
		
		[STAThread]
		public static void Main() {
			CrashReporter.Bind();

			if (!Environment.Is64BitOperatingSystem) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Burning Knight can't run on 32 bit OS, sorry :(");
				Console.ForegroundColor = ConsoleColor.White;

				return;
			}

			KillPreviousInstance();
			
			TryToRemove("burning_log.txt");
			TryToRemove("crashes.txt");

			try {
				SoundEffect.Initialize();
				Console.WriteLine("SoundEffect.Initialize() went ok");
			} catch (Exception e) {
				Assets.FailedToLoadAudio = true;
				Assets.LoadSfx = false;
				Assets.LoadMusic = false;
				
				Console.WriteLine($"Failed: {e}");
			}

			try
			{
				using var game = new DesktopApp();
				game.Run();
			} catch (Exception e) {
				CrashReporter.Report(e);
			} finally {
				RemoveOwnInstance();
			}
		}
	}
}