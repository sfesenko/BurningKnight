using ImGuiNET;
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
		
		// Display-only: the progress bar may read a slightly stale value; the hand-off is `ready`.
		private int progress;

		// Set by the loading worker; the main thread only reads it to decide when the loaded
		// area may be touched. Volatile: the write publishes `gameArea` and everything in it.
		private volatile bool ready;

		// Set by the worker once the saves are in, applied by the main thread.
		private volatile bool checkFullscreen;
		private Area gameArea = null!;
		private float t;
		
		public override void Init() {
			base.Init();
			
			progress = 0;
			Log.Info("Init: progress = 0");

			// Update polls 'progress' while this loads; `ready` is the hand-off. Background: the
			// worker can block on the main thread's GPU queue and must not pin the process.
			new Thread(Load) { IsBackground = true }.Start();
		}
		
		private void Load() {
			Log.Info("Starting asset loading thread");

			SaveManager.Load(gameArea, SaveType.Global);
			
			checkFullscreen = true;
			progress++;
			
			Assets.Load(ref progress);

			Dialogs.Load(); 
			progress++;
			CommonAse.Load();
			
			progress++;
			ImGuiHelper.BindTextures();
			
			progress++;
			Shaders.Load();
			progress++;
			Prefabs.Load();
			progress++;
			Items.Load();
			progress++;
			LootTables.Load();
			progress++;
			Mods.Load();
			;
			progress++; // Should be 13 here

			Log.Info("Done loading assets! Loading level now.");
			
			Lights.Init();
			Physics.Init();

			gameArea = new GameArea();

			Context.Level = null;
			Tilesets.Load();
			;
			progress++;
			
			Achievements.Load();

			if (!LoadEditor)
			{
				SaveManager.Load(gameArea, SaveType.Game); 
				progress++;

				Rnd.Seed = $"{Context.Run.Seed}_{Context.Run.Depth}";
				SaveManager.Load(gameArea, SaveType.Level);
				progress++;

				if (Context.Run.Depth > 0) {
					SaveManager.Load(gameArea, SaveType.Player);
				} else
				{
					SaveManager.Generate(gameArea, SaveType.Player);
				}
			}

			progress++; // Should be 18 here

			Engine.AssetsLoaded?.Invoke();
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