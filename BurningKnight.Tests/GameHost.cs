using System;
using System.IO;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.loot;
using BurningKnight.assets.prefabs;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.util.math;

namespace BurningKnight.Tests;

// Boots the game far enough to generate levels: a real GraphicsDevice (Xvfb on a headless
// machine), the content root, and the loaders AssetLoadState runs minus saves, mods and audio.
// Nothing is rendered; the device is needed because generation places entities, and their
// components fetch sprites.
public static class GameHost {
	private static bool ready;

	public static Area Area { get; private set; }

	public static void Boot() {
		if (ready) {
			return;
		}

		ready = true;

		var root = ContentRoot();
		Assets.SetRoot(root);
		// The same layers ContentRoot builds, minus the archive: generated content first, then
		// the source tree. The animation sheets and the shaders live in bin/.
		Assets.SetSource(new LayeredContentSource([
			new FileContentSource(Path.Combine(root, "bin")),
			new FileContentSource(root)
		]));

		// Generation needs no sound, and the test host has no audio device.
		Assets.LoadSfx = false;
		Assets.LoadMusic = false;

		// What BK's constructor supplies to the engine.
		Lens.Display.Setup(BurningKnight.Display.Width, BurningKnight.Display.Height,
			BurningKnight.Display.UiScale);

		var engine = new TestEngine();
		engine.RunOneFrame();

		// The game loads these on its loading thread; a test keeps them on the main thread, where
		// the GL context lives — a texture upload from a worker while the main thread waits on it
		// hangs.
		Locale.Load(Locale.PrefferedClientLanguage);
		Effects.Load();
		Textures.Load();
		Animations.Load();
		Font.Load();
		CommonAse.Load();
		Shaders.Load();
		Dialogs.Load();
		Prefabs.Load();
		Items.Load();
		LootTables.Load();
		Achievements.Load();
		Lights.Init();
		Physics.Init();
		Tilesets.Load();

		Area = new Area();
	}

	// The test binary sits in <repo>/BurningKnight.Tests/bin/<config>/net10.0, so the repository
	// root is found the same way the game finds it: walk up to the marker.
	private static string ContentRoot() {
		var dir = new DirectoryInfo(AppContext.BaseDirectory);

		for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent) {
			if (File.Exists(Path.Combine(dir.FullName, "global.json"))
			    && Directory.Exists(Path.Combine(dir.FullName, "Content"))) {
				return Path.Combine(dir.FullName, "Content");
			}
		}

		throw new InvalidOperationException("Could not find the repository root from " + AppContext.BaseDirectory);
	}
}
