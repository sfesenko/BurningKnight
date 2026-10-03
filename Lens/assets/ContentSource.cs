using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Lens.assets {
	// Content is read through a source, never through a path. The same loaders serve the source
	// tree during development and a packaged archive in a release, and a layered source puts loose
	// files first, so an override next to the executable wins over what shipped.
	public interface IContentSource : IDisposable {
		bool Exists(string path);

		// Null when the path is missing.
		Stream? Open(string path);

		// The immediate children of a directory; a directory's name ends with '/'.
		IEnumerable<string> List(string path);
	}

	// The seam speaks one dialect: forward slashes, no leading or trailing separator, no "./"
	// prefix, no interior "." or empty segments. Callers and archives can then spell a path the
	// same way.
	internal static class ContentPath {
		public static string Normalize(string path) {
			if (string.IsNullOrEmpty(path)) {
				return "";
			}

			var normalized = path.Replace('\\', '/');

			while (normalized.StartsWith("./", StringComparison.Ordinal)) {
				normalized = normalized[2..];
			}

			var parts = normalized.Split('/');
			var kept = new List<string>(parts.Length);

			foreach (var part in parts) {
				if (part.Length == 0 || part == ".") {
					continue;
				}

				kept.Add(part);
			}

			return string.Join('/', kept);
		}
	}

	// Plain files under a root — the source tree in development, the loose layer in a release.
	public sealed class FileContentSource(string root) : IContentSource {
		private readonly string root = Path.GetFullPath(root);

		public bool Exists(string path) {
			var full = Resolve(path);

			return File.Exists(full) || Directory.Exists(full);
		}

		public Stream? Open(string path) {
			var full = Resolve(path);

			if (File.Exists(full)) {
				return File.OpenRead(full);
			}

			return null;
		}

		public IEnumerable<string> List(string path) {
			var full = Resolve(path);

			if (!Directory.Exists(full)) {
				return [];
			}

			// Materialized: the caller must see a stable snapshot, not a lazily enumerated
			// directory that changes (or vanishes) under it.
			return Directory.EnumerateFileSystemEntries(full)
				.Select(entry => Directory.Exists(entry) ? Path.GetFileName(entry) + "/" : Path.GetFileName(entry)).ToList();
		}

		private string Resolve(string path) {
			var full = Path.GetFullPath(Path.Combine(root, ContentPath.Normalize(path)));
			var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;

			System.Diagnostics.Debug.Assert(full.Equals(root, StringComparison.Ordinal) ||
				full.StartsWith(prefix, StringComparison.Ordinal),
				$"Content path escapes the root: {path}");

			return full;
		}

		public void Dispose() {
			// Plain files hold no state: nothing to release.
		}
	}

	// A zip of the payload. Everything in it is deflated (see tools/pack-content.fsx, which
	// writes every entry with CompressionLevel.Optimal); entries are buffered to memory on open,
	// so callers never see the raw entry streams. The archive and its stream stay open for the
	// life of the source: an entry stream is only valid while the archive that owns it is alive.
	public sealed class ArchiveContentSource : IContentSource {
		private readonly FileStream file;
		private readonly ZipArchive archive;
		private readonly Dictionary<string, ZipArchiveEntry> entries = new(StringComparer.OrdinalIgnoreCase);
		private bool disposed;

		public ArchiveContentSource(string path) {
			file = File.OpenRead(path);
			archive = new ZipArchive(file, ZipArchiveMode.Read);

			foreach (var entry in archive.Entries) {
				entries[ContentPath.Normalize(entry.FullName)] = entry;
			}
		}

		public bool Exists(string path) {
			var name = ContentPath.Normalize(path);

			if (name.Length == 0) {
				return true;
			}

			if (entries.ContainsKey(name)) {
				return true;
			}

			var prefix = name + "/";

			return entries.Keys.Any(key => key.StartsWith(prefix, StringComparison.Ordinal));
		}

		private readonly object gate = new();

		public Stream? Open(string path) {
			// Zip entries share the archive's base stream, which is not thread-safe
			// (level loads on a worker thread, audio streams on the game thread).
			// Copy under the lock and hand out memory; everything the game loads
			// is consumed whole except music, and one buffered song is affordable.
			lock (gate) {
				if (disposed || !entries.TryGetValue(ContentPath.Normalize(path), out var entry)) {
					return null;
				}

				if (entry.FullName.EndsWith('/')) {
					// A directory entry: no bytes to hand out.
					return null;
				}

				var copy = new MemoryStream((int) entry.Length);
				using var source = entry.Open();
				source.CopyTo(copy);
				copy.Position = 0;

				return copy;
			}
		}

		public IEnumerable<string> List(string path) {
			var prefix = ContentPath.Normalize(path);

			if (prefix.Length > 0) {
				prefix += "/";
			}

			var seen = new HashSet<string>();

			foreach (var key in entries.Keys) {
				if (!key.StartsWith(prefix, StringComparison.Ordinal)) {
					continue;
				}

				var rest = key[prefix.Length..];
				var slash = rest.IndexOf('/');
				var name = slash < 0 ? rest : rest[..(slash + 1)];

				if (seen.Add(name)) {
					yield return name;
				}
			}
		}

		public void Dispose() {
			lock (gate) {
				if (disposed) {
					return;
				}

				disposed = true;
			}

			archive.Dispose();
			file.Dispose();
		}
	}

	// Sources in priority order: the first one with the file serves it, and listings merge so a
	// directory shows both what a mod added and what shipped.
	public sealed class LayeredContentSource(params IContentSource[] layers) : IContentSource {
		public bool Exists(string path) {
			foreach (var layer in layers) {
				if (layer.Exists(path)) {
					return true;
				}
			}

			return false;
		}

		public Stream? Open(string path) {
			foreach (var layer in layers) {
				var stream = layer.Open(path);

				if (stream != null) {
					return stream;
				}
			}

			return null;
		}

		public IEnumerable<string> List(string path) {
			var seen = new HashSet<string>();

			foreach (var layer in layers) {
				foreach (var name in layer.List(path)) {
					if (seen.Add(name)) {
						yield return name;
					}
				}
			}
		}

		public void Dispose() {
			foreach (var layer in layers) {
				layer.Dispose();
			}
		}
	}
}
