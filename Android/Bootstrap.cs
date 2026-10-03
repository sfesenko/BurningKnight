using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using AndroidPort.core;
using Lens;
using Lens.assets;
using Lens.input;
using Lens.util;

namespace AndroidPort;

// The host side of the engine's seams: writable state under FilesDir, content from the APK's
// asset copy, opened as an archive. Everything after this is game behaviour.
public static class Bootstrap {
	private const string ArchiveName = "Content.zip";
	private const string VersionName = "Content.version";

	private const long CrashLogResetBytes = 256 * 1024;
	private const int MaxCrashPayloadChars = 64 * 1024;

	private static readonly object CrashLock = new();

	public static void Setup(Context context) {
		var files = context.FilesDir;

		if (files == null) {
			throw new InvalidOperationException("FilesDir is unavailable, nowhere to put state.");
		}

		var data = files.AbsolutePath;

		Paths.Setup(data);
		Vibration.Instance = new AndroidRumble();
		WatchForCrashes(context, data);

		var archive = Path.Combine(data, ArchiveName);
		var marker = archive + ".version";

		SyncArchive(context, archive, marker);

		Assets.SetRoot(data);

		try {
			Assets.SetSource(new ArchiveContentSource(archive));
		} catch (Exception e) {
			// A corrupt copy (killed mid-unpack, bad flash) would otherwise crash-loop every
			// launch: the marker says current so the copy is never redone. Drop both and unpack
			// once more from the APK; if that fails too, it is genuinely broken, let it throw.
			Log.Error($"Content archive unreadable, re-syncing: {e}");

			try {
				if (File.Exists(archive)) {
					File.Delete(archive);
				}

				if (File.Exists(marker)) {
					File.Delete(marker);
				}
			} catch (Exception deleteError) {
				Log.Error(deleteError);
			}

			SyncArchive(context, archive, marker);
			Assets.SetSource(new ArchiveContentSource(archive));
		}
	}

	// No store, no backend, no crash service on a sideloaded handheld: managed
	// crashes land in a local file instead of vanishing with the process. The
	// game log has the run-up; this has the exception. Send both on a bug report.
	private static void WatchForCrashes(Context context, string data) {
		if (string.IsNullOrEmpty(data)) {
			return;
		}

		var appVersion = AppVersion(context);

		void Write(string kind, object? payload) {
			// The size check and the append are one unit: two crashing threads must not both
			// pass the check and both append past the cap.
			lock (CrashLock) {
				try {
					var path = Path.Combine(data, "crash_log.txt");
					var info = new FileInfo(path);

					if (info.Exists && info.Length > CrashLogResetBytes) {
						File.Delete(path);
					}

					var text = payload?.ToString() ?? "<null>";

					if (text.Length > MaxCrashPayloadChars) {
						text = text.Substring(0, MaxCrashPayloadChars) + "\n…[truncated]";
					}

					File.AppendAllText(path,
						$"--- {kind} {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z {Build.Model} API{Build.VERSION.SdkInt} v{appVersion}\n{text}\n");
				} catch {
					// Nowhere left to report to.
				}
			}
		}

		AppDomain.CurrentDomain.UnhandledException += (_, e) => Write("unhandled", e.ExceptionObject);
		TaskScheduler.UnobservedTaskException += (_, e) => {
			Write("task", e.Exception);
			e.SetObserved();
		};
	}

	// Display version + version code, best effort: a missing PackageManager must not take
	// the crash reporter down with it.
	private static string AppVersion(Context context) {
		try {
			var pm = context.PackageManager;
			var package = context.PackageName;

			if (pm == null || string.IsNullOrEmpty(package)) {
				return "?";
			}

#pragma warning disable CS0618 // The int overload works back to API 1; the Flags one needs API 33.
			var info = pm.GetPackageInfo(package, 0);
#pragma warning restore CS0618

			if (info == null) {
				return "?";
			}

			var display = info.VersionName ?? "?";
			string code;

			if (OperatingSystem.IsAndroidVersionAtLeast(28)) {
				code = info.LongVersionCode.ToString();
			} else {
#pragma warning disable CA1422 // Guarded: this branch runs only below API 28.
				code = info.VersionCode.ToString();
#pragma warning restore CA1422
			}

			return $"{display} ({code})";
		} catch {
			return "?";
		}
	}

	// The APK carries one archive; it is copied out once per packed content, not on every
	// launch. The version asset is the archive's build timestamp, written by the build.
	// The copy is staged (Content.zip.tmp, validated, then renamed): a kill mid-unpack never
	// leaves half an archive behind, and a leftover .tmp is a killed unpack, dropped on boot.
	private static void SyncArchive(Context context, string archive, string marker) {
		try {
			var stale = archive + ".tmp";

			if (File.Exists(stale)) {
				File.Delete(stale);
			}
		} catch (Exception e) {
			Log.Error(e);
		}

		var version = ReadAsset(context, VersionName);
		string? marked = null;

		try {
			// An unreadable marker just means unpack again; it must not kill boot.
			if (File.Exists(archive) && version != null && File.Exists(marker)) {
				marked = File.ReadAllText(marker);
			}
		} catch (Exception e) {
			Log.Error(e);
		}

		if (marked != null && marked == version) {
			return;
		}

		var tmp = archive + ".tmp";

		try {
			using var source = context.Assets!.Open(ArchiveName);
			using var destination = File.Create(tmp);

			source.CopyTo(destination);
		} catch (Exception e) {
			Log.Error($"Failed to unpack {ArchiveName}: {e}");

			try {
				if (File.Exists(tmp)) {
					File.Delete(tmp);
				}
			} catch {
				// Stale .tmp is dropped on the next boot.
			}

			return;
		}

		// Validate before it replaces anything: opening parses the central directory.
		try {
			using var stream = File.OpenRead(tmp);
			using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

			_ = zip.Entries.Count;
		} catch (Exception e) {
			Log.Error($"Unpacked {ArchiveName} is corrupt, discarding: {e}");

			try {
				File.Delete(tmp);
			} catch {
				// Stale .tmp is dropped on the next boot.
			}

			return;
		}

		try {
			// Same directory, so the rename replaces atomically.
			File.Move(tmp, archive, true);

			if (version != null) {
				File.WriteAllText(marker, version);
			}

			Log.Info($"Unpacked {ArchiveName} ({new FileInfo(archive).Length} bytes)");
		} catch (Exception e) {
			Log.Error($"Failed to unpack {ArchiveName}: {e}");
		}
	}

	private static string? ReadAsset(Context context, string name) {
		try {
			using var stream = context.Assets!.Open(name);
			using var reader = new StreamReader(stream);

			return reader.ReadToEnd().Trim();
		} catch (Exception) {
			return null;
		}
	}
}
