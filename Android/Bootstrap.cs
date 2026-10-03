using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
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

	public static void Setup(Context context) {
		var data = context.FilesDir!.AbsolutePath;

		Paths.Setup(data);
		Vibration.Instance = new AndroidRumble();
		WatchForCrashes(data);

		var archive = Path.Combine(data, ArchiveName);
		SyncArchive(context, archive);

		Assets.SetRoot(data);
		Assets.SetSource(new ArchiveContentSource(archive));
	}

	// No store, no backend, no crash service on a sideloaded handheld: managed
	// crashes land in a local file instead of vanishing with the process. The
	// game log has the run-up; this has the exception. Send both on a bug report.
	private static void WatchForCrashes(string data) {
		void Write(string kind, object payload) {
			try {
				var path = Path.Combine(data, "crash_log.txt");
				var info = new FileInfo(path);

				if (info.Exists && info.Length > 256 * 1024) {
					File.Delete(path);
				}

				File.AppendAllText(path,
					$"--- {kind} {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z {Android.OS.Build.Model} API{Android.OS.Build.VERSION.SdkInt}\n{payload}\n");
			} catch {
				// Nowhere left to report to.
			}
		}

		AppDomain.CurrentDomain.UnhandledException += (_, e) => Write("unhandled", e.ExceptionObject);
		TaskScheduler.UnobservedTaskException += (_, e) => {
			Write("task", e.Exception);
			e.SetObserved();
		};
	}

	// The APK carries one archive; it is copied out once per packed content, not on every
	// launch. The version asset is the archive's build timestamp, written by the build.
	private static void SyncArchive(Context context, string archive) {
		var version = ReadAsset(context, VersionName);
		var marker = archive + ".version";

		if (File.Exists(archive) && version != null && File.Exists(marker) && File.ReadAllText(marker) == version) {
			return;
		}

		try {
			using var source = context.Assets!.Open(ArchiveName);
			using var destination = File.Create(archive);

			source.CopyTo(destination);

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
