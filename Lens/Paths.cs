using System.IO;

namespace Lens;

// Where the game keeps writable state. The host supplies it; the engine and the game must not
// guess it from the environment.
public static class Paths {
	public static string DataDir { get; private set; } = "";

	public static void Setup(string dataDir) {
		DataDir = Path.EndsInDirectorySeparator(dataDir) ? dataDir : dataDir + Path.DirectorySeparatorChar;
	}
}
