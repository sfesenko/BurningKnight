using System.IO;

namespace Lens;

// Where the game keeps writable state. The host supplies it; the engine and the game must not
// guess it from the environment.
public static class Paths {
	public static string DataDir { get; private set; } = "";

	// Where the host kept it before, when that has changed. Null when there is nowhere to
	// migrate from.
	public static string? LegacyDataDir { get; private set; }

	public static void Setup(string dataDir, string? legacyDataDir = null) {
		DataDir = EndWithSeparator(dataDir);
		LegacyDataDir = legacyDataDir;
	}

	private static string EndWithSeparator(string path) {
		return Path.EndsInDirectorySeparator(path) ? path : path + Path.DirectorySeparatorChar;
	}
}
