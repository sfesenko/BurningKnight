using System;
using System.IO;
using BurningKnight.save;
using Lens;

namespace BurningKnight.Tests;

// A throwaway data directory for the save tests. Paths must point at it before SaveManager is
// first touched: its SlotDir is a static field captured from the data directory at that moment.
public class SaveHost : IDisposable {
	public string Root { get; }

	public SaveHost() {
		GameHost.Boot();

		Root = Path.Combine(Path.GetTempPath(), "bk-save-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(Root);

		Paths.Setup(Root);
		SaveManager.Init();
	}

	// A fresh directory with the trailing separator the savers expect.
	public string NewDir(string name) {
		var dir = Path.Combine(Root, name);
		Directory.CreateDirectory(dir);

		return dir + Path.DirectorySeparatorChar;
	}

	public void Dispose() {
		try {
			Directory.Delete(Root, true);
		} catch {
			// Best effort: a leaked temp directory must not fail the suite.
		}
	}
}
