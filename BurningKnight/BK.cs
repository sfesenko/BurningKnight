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
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight;

public class BK : Engine {
	public const bool StandMode = false;
	public const bool Demo = false;

	protected BK(int width, int height, bool fullscreen) : base(
		Rnd.Chance(60) ? "Burning Knight" : $"Burning Knight{(Demo ? " Demo" : "")}: {Titles.Generate()}", width, height, fullscreen) {
	}

	protected override void Initialize() {
		base.Initialize();

		SaveManager.Init();
		Controls.Load();
		ImGuiHelper.Init();
		Weather.Init();
	}

	protected override void LoadContent()
	{
		base.LoadContent();

		NewState = Debug
			? new DevAssetLoadState()
			: new AssetLoadState();

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