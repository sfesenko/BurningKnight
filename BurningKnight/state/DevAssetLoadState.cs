using System;
using System.Threading;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.loot;
using BurningKnight.assets.mod;
using BurningKnight.assets.prefabs;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.game;
using Lens.graphics;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurningKnight.state {
	public class DevAssetLoadState : GameState {
		private const bool LoadEditor = false;
		public const bool LoadCutscene = !LoadEditor && false;
		
		private int progress;
		private bool ready;
		private bool checkFullscreen;
		private Area gameArea;
		private float t;
		
		public override void Init() {
			base.Init();
			
			progress = 0;
			Log.Info("Init: progress = 0");

			AsyncUtils.RunAsync("Load", Load);
		}
		
		private void Load() {
			Log.Info("Starting asset loading thread");

			AsyncUtils.RunSync("SaveManager.Load", () => SaveManager.Load(gameArea, SaveType.Global) );
			
			checkFullscreen = true;
			progress++;
			
			AsyncUtils.RunSync("Assets.Load", () => Assets.Load(ref progress) );

			AsyncUtils.RunSync("Dialogs.Load", Dialogs.Load ); 
			progress++;
			AsyncUtils.RunSync("CommonAse.Load()", CommonAse.Load );
			
			progress++;
			AsyncUtils.RunSync("ImGuiHelper.BindTextures()", ImGuiHelper.BindTextures );
			
			progress++;
			AsyncUtils.RunSync("Shaders.Load()", Shaders.Load );
			progress++;
			AsyncUtils.RunSync("Prefabs.Load()", Prefabs.Load );
			progress++;
			AsyncUtils.RunSync("Items.Load()", Items.Load );
			progress++;
			AsyncUtils.RunSync("LootTables.Load()", LootTables.Load );
			progress++;
			AsyncUtils.RunSync("Mods.Load()", Mods.Load );
			;
			progress++; // Should be 13 here

			Log.Info("Done loading assets! Loading level now.");
			
			AsyncUtils.RunSync("Lights.Init()", Lights.Init );
			AsyncUtils.RunSync("Physics.Init()", Physics.Init );

			gameArea = new Area();

			Run.Level = null;
			AsyncUtils.RunSync("Tilesets.Load()", Tilesets.Load );
			;
			progress++;
			
			AsyncUtils.RunSync("Achievements.Load()", Achievements.Load );

			if (!LoadEditor)
			{
				AsyncUtils.RunSync("SaveManager.Load", () => SaveManager.Load(gameArea, SaveType.Game)); 
				progress++;

				Rnd.Seed = $"{Run.Seed}_{Run.Depth}";
				AsyncUtils.RunSync("SaveManager.Load", () => SaveManager.Load(gameArea, SaveType.Level));
				progress++;

				if (Run.Depth > 0) {
					AsyncUtils.RunSync("SaveManager.Load", () => SaveManager.Load(gameArea, SaveType.Player));
				} else
				{
					AsyncUtils.RunSync("SaveManager.Generate", () => SaveManager.Generate(gameArea, SaveType.Player));
				}
			}

			progress++; // Should be 18 here

			AsyncUtils.RunSync("Engine.AssetsLoaded?.Invoke()", () => Engine.AssetsLoaded?.Invoke());
			ready = true;
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (checkFullscreen) {
				checkFullscreen = false;
				
				if (Settings.Fullscreen) {
					Engine.Instance.SetFullscreen();
				} else {
					Engine.Instance.SetWindowed(Display.Width * 3, Display.Height * 3);
				}
			}

			t += dt;
			
			if (ready) {
				if (Settings.Fullscreen) {
					Engine.Instance.SetFullscreen();
				} else {
					Engine.Instance.SetWindowed(Display.Width * 3, Display.Height * 3);
				}

				Engine.Instance.StateRenderer.UiEffect = Shaders.Ui;
				Engine.Instance.SetState(LoadEditor ? (GameState) new EditorState() : (LoadCutscene ? (GameState) new LoadState {
					IntoCutscene = true
				} : new InGameState(gameArea, false)));
			}
		}

		public override void RenderUi() {
			base.RenderUi();
			Graphics.Print($"Loading assets {progress / 18f * 100}%", Font.Small, new Vector2(10, 10));
			
			var n = t % 2f;
			var s = $"{(n > 0.5f ? "." : "")}{(n > 1f ? "." : "")}{(n > 1.5f ? "." : "")}";
			
			var x = Font.Small.MeasureString(s).Width * -0.5f;
			Graphics.Print(s, Font.Small, new Vector2(Display.UiWidth / 2f + x, Display.UiHeight / 2f + 32));
		}
	}
}