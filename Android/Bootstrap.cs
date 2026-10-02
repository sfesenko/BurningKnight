using System;
using System.IO;
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

		var archive = Path.Combine(data, ArchiveName);
		SyncArchive(context, archive);

		Assets.SetRoot(data);
		Assets.SetSource(new ArchiveContentSource(archive));
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
