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
		Assets.SetSource(OpenApkArchive(context));
	}

	// Content.zip ships stored (not deflated — see the csproj flag), so its bytes sit
	// verbatim in base.apk. OpenFd gives the offset/length; a bounded stream over the
	// APK file serves ZipArchive's seeks with no copy and no duplication. Throws when
	// the asset is compressed instead of stored — that means the packaging regressed.
	private static ArchiveContentSource OpenApkArchive(Context context) {
		long offset;
		long length;

		using (var descriptor = context.Assets!.OpenFd(ArchiveName)) {
			offset = descriptor.StartOffset;
			length = descriptor.Length;
		}

		var apk = context.ApplicationInfo!.SourceDir!;

		var file = new FileStream(apk, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.RandomAccess);
		var region = new ApkRegionStream(file, offset, length);

		try {
			var source = new ArchiveContentSource(region);

			Log.Info($"Opened {ArchiveName} in place ({length} bytes at offset {offset})");

			return source;
		} catch {
			region.Dispose();

			throw;
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

}
