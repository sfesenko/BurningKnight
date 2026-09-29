using System;
using System.Collections.Generic;
using System.IO;
using Lens;
using Lens.assets;

namespace Desktop {
	// The host decides where content lives; the engine is told, never left to guess from the
	// working directory. The order is an explicit override, then content staged next to the
	// executable, then the source tree found by walking up to the repository root.
	public static class ContentRoot {
		public const string EnvVar = "BK_CONTENT";
		public const string ArchiveName = "Content.zip";

		private const int MaxDepth = 6;
		private const string RepoMarker = "global.json";
		private const string SourceContent = "Content";

		// Reads resolve through layers: generated content first, then the source tree, then the
		// packaged archive. Loose files win over the archive, so a mod or an override dropped into
		// Content/ still takes effect. Debug never reads the archive — a stale one beside a
		// development build would only shadow the source tree.
		public static IContentSource BuildSource(string root) {
			var layers = new List<IContentSource> {
				new FileContentSource(Path.Combine(root, "bin")),
				new FileContentSource(root)
			};

			var archive = Path.Combine(AppContext.BaseDirectory, ArchiveName);

			if (!Engine.Debug && File.Exists(archive)) {
				Console.WriteLine($"Content archive: {archive}");
				layers.Add(new ArchiveContentSource(archive));
			}

			return new LayeredContentSource([.. layers]);
		}

		public static string Resolve() {
			var fromEnv = Environment.GetEnvironmentVariable(EnvVar);

			if (!string.IsNullOrEmpty(fromEnv) && Directory.Exists(fromEnv)) {
				Console.WriteLine($"Content root: {fromEnv} (from {EnvVar})");
				return fromEnv;
			}

			var staged = Path.Combine(AppContext.BaseDirectory, "Content");

			if (Directory.Exists(staged)) {
				Console.WriteLine($"Content root: {staged} (staged next to the executable)");
				return staged;
			}

			// Only a development build walks up to the source tree. A release install that cannot
			// find its staged content must not quietly read the repository instead — loose files
			// would win over the archive.
			if (Engine.Debug) {
				var dir = new DirectoryInfo(AppContext.BaseDirectory);

				for (var i = 0; i < MaxDepth && dir != null; i++, dir = dir.Parent) {
					var source = Path.Combine(dir.FullName, SourceContent);

					if (File.Exists(Path.Combine(dir.FullName, RepoMarker)) && Directory.Exists(source)) {
						Console.WriteLine($"Content root: {source} (source tree, {i + 1} levels up)");
						return source;
					}
				}
			}

			Console.WriteLine($"Content root: {staged} (not found; expected it here)");

			return staged;
		}

		// A development build links the source tree beside the executable, so a `Content` directory
		// is there for anything that opens a path rather than a stream — a music URI, the overlay's
		// font. A release install reads its archive instead and never links: a link back to a
		// checkout would shadow the archive.
		public static void LinkNextToExecutable(string root) {
			if (!Engine.Debug) {
				return;
			}

			var link = Path.Combine(AppContext.BaseDirectory, "Content");

			if (Directory.Exists(link)) {
				return;
			}

			try {
				// A leftover entry here — a link from an earlier layout, say — still blocks a new
				// link, and a dangling one reports no Directory.Exists while still being present.
				if (Path.Exists(link) && !Directory.Exists(link)) {
					Unlink(link);
				}

				Directory.CreateSymbolicLink(link, Path.GetRelativePath(AppContext.BaseDirectory, root));
				Console.WriteLine($"Linked {link} -> {root}");
			} catch (Exception e) {
				Console.WriteLine($"Could not link content beside the executable: {e.Message}");
			}
		}

		// Removes whatever occupies a path without following it. Unix unlinks the entry itself;
		// Windows treats a directory link as a directory even when its target is gone, and wants
		// the directory call.
		private static void Unlink(string path) {
			if (OperatingSystem.IsWindows()) {
				Directory.Delete(path);
			} else {
				File.Delete(path);
			}
		}
	}
}
