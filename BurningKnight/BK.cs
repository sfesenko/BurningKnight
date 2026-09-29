using System;
using BurningKnight.assets;
using BurningKnight.assets.input;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.mod;
using BurningKnight.assets.prefabs;
using BurningKnight.level;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Lens.core;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight;

public partial class BK : Engine {
	public const bool StandMode = false;
	public const bool Demo = false;

	protected BK(int width, int height, bool fullscreen, Func<Core> coreFactory) : base(
		Rnd.Chance(60) ? "Burning Knight" : $"Burning Knight{(Demo ? " Demo" : "")}: {Titles.Generate()}", width, height, fullscreen, coreFactory) {
		// Display reads as 0 until this runs, so nothing before it — including Engine's
		// constructor — may use it.
		Lens.Display.Setup(Display.Width, Display.Height, Display.UiScale);
	}

	// Implemented in BK.Debug.cs: a release build must not start the overlay, so it neither
	// reads its font nor keeps the renderer alive (ADR-0003).
	partial void InitDebugOverlay();

	protected override void Initialize() {
		base.Initialize();

		SaveManager.Init();
		Controls.Load();
		InitDebugOverlay();
		Weather.Init();
	}

	protected override void LoadContent()
	{
		base.LoadContent();

#if DEBUG
		NewState = new DevAssetLoadState();
#else
		NewState = new AssetLoadState();
#endif

		Font.Load();
	}

	protected override void UnloadContent() {
		Mods.Destroy();
		Items.Destroy();
		Prefabs.Destroy();
		Lights.DestroySurface();
			
		base.UnloadContent();
	}

	protected override void Update(GameTime gameTime) {
		base.Update(gameTime);
		Mods.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
	}

	public override void RenderUi() {
		base.RenderUi();
		Mods.Render();
	}
}