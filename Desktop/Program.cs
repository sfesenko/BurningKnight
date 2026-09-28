using System;
using System.Diagnostics;
using System.IO;
using BurningKnight;
using Desktop.integration.crash;
using Desktop.services;
using Lens;
using Lens.assets;
using Lens.services;
using Microsoft.Xna.Framework.Audio;

namespace Desktop {
	public class Program {
		// The game's name for its writable state; a demo build keeps its own.
		private static readonly string StateName = BK.Demo ? "burning_knight_demo" : "burning_knight";

		// Writable state lives in the user's data directory, not next to the executable: a
		// staged install may be read-only.
		private static readonly string DataDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "." + StateName) + Path.DirectorySeparatorChar;

		// Where this build kept it before the move, for the game to migrate from: beside the
		// executable.
		private static readonly string LegacyDataDir = Path.Combine(AppContext.BaseDirectory, StateName);

		// The game records its own pid here so the next launch can stop it.
		private static readonly string InstanceFile = Path.Combine(DataDir, "instance.pid");

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
			Paths.Setup(DataDir, LegacyDataDir);

			Clipboard.Instance = new DesktopClipboard();
			CloudSave.Instance = SteamCloudSave.Instance;
			Stats.Instance = new SteamStats();

			CrashReporter.Bind();

			if (!Environment.Is64BitOperatingSystem) {
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Burning Knight can't run on 32 bit OS, sorry :(");
				Console.ForegroundColor = ConsoleColor.White;

				return;
			}

			KillPreviousInstance();

			// The engine takes the content root explicitly, but the working directory is set as
			// well: MonoGame's TitleContainer — and MonoGame.Extended's BitmapFont, which uses it
			// — only accepts paths relative to the working directory.
			var content = ContentRoot.Resolve();

			Assets.SetRoot(content);
			Assets.SetSource(ContentRoot.BuildSource(content));
			ContentRoot.LinkNextToExecutable(content);
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(content)));
			
			TryToRemove(Path.Combine(DataDir, "burning_log.txt"));
			TryToRemove(Path.Combine(DataDir, "crashes.txt"));

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