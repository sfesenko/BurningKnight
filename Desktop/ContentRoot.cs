using System;
using System.IO;

namespace Desktop {
	// The host decides where content lives; the engine is told, never left to guess from the
	// working directory. The order is an explicit override, then content staged next to the
	// executable, then the source tree found by walking up to the repository root.
	public static class ContentRoot {
		public const string EnvVar = "BK_CONTENT";

		private const int MaxDepth = 6;
		private const string RepoMarker = "global.json";
		private const string SourceContent = "BurningKnight/Content";

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

			var dir = new DirectoryInfo(AppContext.BaseDirectory);

			for (var i = 0; i < MaxDepth && dir != null; i++, dir = dir.Parent) {
				var source = Path.Combine(dir.FullName, SourceContent);

				if (File.Exists(Path.Combine(dir.FullName, RepoMarker)) && Directory.Exists(source)) {
					Console.WriteLine($"Content root: {source} (source tree, {i + 1} levels up)");
					return source;
				}
			}

			Console.WriteLine($"Content root: {staged} (not found; expected it here)");

			return staged;
		}

		// MonoGame's TitleContainer resolves relative paths against the executable's directory —
		// not the content root, and not the working directory. The ContentManager and
		// MonoGame.Extended's BitmapFont both go through it, so content has to be reachable from
		// there too. Staging puts it there; a Debug build links to the source tree instead.
		public static void LinkNextToExecutable(string root) {
			var link = Path.Combine(AppContext.BaseDirectory, "Content");

			if (Directory.Exists(link)) {
				return;
			}

			try {
				Directory.CreateSymbolicLink(link, Path.GetRelativePath(AppContext.BaseDirectory, root));
				Console.WriteLine($"Linked {link} -> {root}");
			} catch (Exception e) {
				Console.WriteLine($"Could not link content beside the executable: {e.Message}");
			}
		}
	}
}
