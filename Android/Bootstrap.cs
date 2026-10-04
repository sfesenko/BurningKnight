using System;
using System.IO;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using AndroidPort.core;
using Lens;
using Lens.assets;
using Lens.input;
using Lens.util;

namespace AndroidPort;

// The host side of the engine's seams: writable state under FilesDir, content read in
// place from the APK. Everything after this is game behaviour.
public static class Bootstrap {
	private const string ArchiveName = "Content.zip";

	private const long CrashLogResetBytes = 256 * 1024;
	private const int MaxCrashPayloadChars = 64 * 1024;

	private static readonly object CrashLock = new();
	private static bool crashHandlersInstalled;

	public static void Setup(Context context) {
		var files = context.FilesDir;

		if (files == null) {
			throw new InvalidOperationException("FilesDir is unavailable, nowhere to put state.");
		}

		var data = files.AbsolutePath;

		Paths.Setup(data);
		Vibration.Instance = new AndroidRumble();
		WatchForCrashes(context, data);

		Assets.SetRoot(data);

		var previous = Assets.Source;
		var source = OpenApkArchive(context);
		Assets.SetSource(source);

		if (!ReferenceEquals(previous, source)) {
			// First boot owns a FileContentSource (no state); a recreated activity owns
			// the previous APK source. Never leak the APK fd across recreations.
			try {
				previous.Dispose();
			} catch (Exception e) {
				Log.Error(e);
			}
		}
	}

	// Content.zip ships stored (not deflated — see the csproj flag), so its bytes sit
	// verbatim in base.apk. OpenFd gives the offset/length; a bounded stream over the
	// APK file serves ZipArchive's seeks with no copy and no duplication. Throws when
	// the asset is compressed instead of stored — that means the packaging regressed.
	private static ArchiveContentSource OpenApkArchive(Context context) {
		if (context.Assets == null) {
			throw new InvalidOperationException("AssetManager is unavailable, cannot open Content.zip.");
		}

		long offset;
		long length;

		try {
			using (var descriptor = context.Assets.OpenFd(ArchiveName)) {
				offset = descriptor.StartOffset;
				length = descriptor.Length;
			}
		} catch (Exception e) {
			throw new InvalidOperationException(
				$"Content.zip is missing or compressed in the APK (expected a stored asset): {e.Message}", e);
		}

		if (length <= 0) {
			throw new InvalidOperationException(
				$"Content.zip in APK has length {length}: expected a stored (uncompressed) asset — check AndroidStoreUncompressedFileExtensions.");
		}

		if (offset < 0) {
			throw new InvalidOperationException($"Content.zip offset {offset} is invalid.");
		}

		var apk = context.ApplicationInfo?.SourceDir;

		if (string.IsNullOrEmpty(apk)) {
			throw new InvalidOperationException("APK path (SourceDir) is unavailable.");
		}

		FileStream? file = null;
		ApkRegionStream? region = null;

		try {
			file = new FileStream(apk, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
			region = new ApkRegionStream(file, offset, length);
			var source = new ArchiveContentSource(region);

			Log.Info($"Opened {ArchiveName} in place ({length} bytes at offset {offset})");

			return source;
		} catch (Exception e) {
			// Region owns the file once constructed; anything earlier owns nothing,
			// so each layer is disposed only if the next one never took it.
			if (region != null) {
				region.Dispose();
			} else {
				file?.Dispose();
			}

			throw new InvalidOperationException($"Content.zip at offset {offset} ({length} bytes) is unreadable: {e.Message}", e);
		}
	}

	// No store, no backend, no crash service on a sideloaded handheld: managed
	// crashes land in a local file instead of vanishing with the process. The
	// game log has the run-up; this has the exception. Send both on a bug report.
	private static void WatchForCrashes(Context context, string data) {
		if (string.IsNullOrEmpty(data)) {
			return;
		}

		lock (CrashLock) {
			// A recreated activity runs Setup again: never double-subscribe.
			if (crashHandlersInstalled) {
				return;
			}

			crashHandlersInstalled = true;
		}

		var appVersion = AppVersion(context);

		AppDomain.CurrentDomain.UnhandledException += (_, e) => WriteCrash(data, appVersion, "unhandled", e.ExceptionObject);
		TaskScheduler.UnobservedTaskException += (_, e) => {
			WriteCrash(data, appVersion, "task", e.Exception);
			e.SetObserved();
		};
	}

	// A synchronous boot failure (content missing, engine ctor throw) is caught, not
	// unhandled, so the handlers above never fire for it — but burning_log.txt does not
	// exist yet either (Log.Open runs in Engine.Initialize). The activity mirrors those
	// catches here so the file the README asks for in bug reports actually exists.
	public static void WriteCrash(Context context, string kind, object? payload) {
		// The crash reporter must never be able to kill the catch that calls it:
		// a throwing FilesDir/AbsolutePath here would skip ShowFailure and turn
		// a handled boot failure into an unhandled crash with no log entry.
		try {
			var files = context.FilesDir;

			if (files == null || string.IsNullOrEmpty(files.AbsolutePath)) {
				return;
			}

			WriteCrash(files.AbsolutePath, AppVersion(context), kind, payload);
		} catch {
			// Nowhere left to report to.
		}
	}

	private static void WriteCrash(string data, string appVersion, string kind, object? payload) {
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

}
