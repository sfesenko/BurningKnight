using System;
using System.Diagnostics;
using System.Threading;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.input;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.loot;
using BurningKnight.assets.mod;
using BurningKnight.assets.prefabs;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.ui.str;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.game;
using Lens.graphics;
using Lens.input;
using Lens.util;
using Lens.util.math;
using Lens.util.tween;
using Microsoft.Xna.Framework;

namespace BurningKnight.state {
	public class AssetLoadState : GameState {
		private PhotoCard logoCard = null!;

		private TextureRegion pixel = null!;
		// private PhotoCard[] cards = new PhotoCard[3];

		// Set by the loading worker; the main thread only reads it to decide when the loaded
		// area may be touched. Volatile: the write publishes `gameArea` and everything in it.
		private volatile bool ready;

		// Display-only: the progress bar may read a slightly stale value; it is also passed by
		// ref into the loader, so it cannot be volatile. The hand-off is `ready`.
		private int progress;

		private Area gameArea = null!;
		private float t;
		private bool added;
		private bool removed;

		// Set by the worker once the saves are in, applied by the main thread.
		private volatile bool checkFullscreen;

		// Set by the worker when loading throws: `ready` stays false, so without this
		// the screen would load forever with no error. The game thread surfaces it.
		private volatile bool failed;
		private string failure = "";

		private float lastV;
		private UiString tipLabel = null!;
		private bool exitTweenDone;
		private string currentlyLoadingLabel = null!;

		private static readonly bool SectionalLoadTimeLogging = false;
		
		public override void Init() {
			base.Init();

			pixel = new TextureRegion(Textures.FastLoad("pixel.png"));
			
			Ui.Add(logoCard = new PhotoCard {
				Region = new TextureRegion(Textures.FastLoad("logo.png")),
				Url = "https://twitter.com/rexcellentgames",
				Name = "Rexcellent Games",
				Position = new Vector2(240, Display.UiHeight + 60),
				Target = new Vector2(240, 115),
				Scale = 1
			});

			Ui.Add(tipLabel = new UiString(Font.Small));

			// The tips are localized, so the locale has to be in before the first one is picked;
			// Assets.Load would load it later, on the worker.
			if (Locale.Map == null) {
				Locale.Load(Locale.PrefferedClientLanguage);
			}

			GenerateNewTip();
			
			logoCard.Start.Y = -90;
			progress = 0;

			var thread = new Thread(() => {
				try {
					var sw = Stopwatch.StartNew();

					Log.Info("Starting asset loading thread");

					LoadSection(() => SaveManager.Load(gameArea, SaveType.Global), "Global saves");
					
					checkFullscreen = true;

					if (Assets.LoadMusic) {
						LoadSection(() => Context.Audio.PlayMusic("Void"), "Audio");
					} else {
						progress++;
					}

					LoadSection(() => Assets.Load(ref progress), "Assets");
					progress++;

					LoadSection(Dialogs.Load, "Dialogs");

					LoadSection(CommonAse.Load, "Common");
					
					LoadSection(Shaders.Load, "Shaders");
					LoadSection(Prefabs.Load, "Prefabs");
					LoadSection(Items.Load, "Items");
					LoadSection(LootTables.Load, "Loot tables");
					LoadSection(Mods.Load, "Mods");
					
					Log.Info("Done loading assets! Loading level now.");
				
					LoadSection(() => {
						Lights.Init();
						Physics.Init();
					}, "Lights & physics");
					
					gameArea = new GameArea();
					Context.Level = null;

					LoadSection(Tilesets.Load, "Tilesets");
					LoadSection(Achievements.Load, "Achievements");
					
					LoadSection(() => {
						SaveManager.Load(gameArea, SaveType.Game);
					}, "Game saves");

					Rnd.Seed = $"{Context.Run.Seed}_{Context.Run.Depth}";

					LoadSection(() => {
						SaveManager.Load(gameArea, SaveType.Level);
					}, "Level saves");
					
					LoadSection(() => {
						if (Context.Run.Depth > 0) {
							SaveManager.Load(gameArea, SaveType.Player);
						} else {
							SaveManager.Generate(gameArea, SaveType.Player);
						}
					}, "Player saves");

					Log.Info($"Done loading level in {sw.ElapsedMilliseconds} ms! Going to menu.");
					
					ready = true;
				} catch (Exception e) {
					// Never die silently on the worker: a throw used to leave `ready`
					// false forever (infinite loading screen). Record it for the game
					// thread to surface; per-asset guards above mean this is now a
					// truly unexpected failure.
					Log.Error("Asset loading failed");
					Log.Error(e);

					failure = e.Message;
					failed = true;
				}
			});

			// The worker can block on the main thread's GPU queue; it must not pin the process
			// open if the window closes mid-load.
			thread.IsBackground = true;
			thread.Start();
		}

		private float tm;

		public override void Update(float dt) {
			base.Update(dt);

			if (failed) {
				// Dead end surfaced, not hung: the label names the failure, and any
				// confirm/back press leaves instead of sitting on a dead screen.
				// UiSelect covers keyboard (Enter/Space/X) and pad (X/A/Y); UiAccept
				// covers the mouse; GameStart/UiBack cover pad Start/Back and Esc.
				if (Input.WasPressed(Controls.GameStart) || Input.WasPressed(Controls.UiSelect) || Input.WasPressed(Controls.UiAccept) || Input.WasPressed(Controls.UiBack)) {
					Engine.Instance.Exit();
				}
			}

			t += dt;
			tm -= dt;

			if (tm <= 0) {
				tm = 10f;
				GenerateNewTip();
			}
			
			if (checkFullscreen) {
				checkFullscreen = false;
				
				if (Engine.Instance.CanToggleFullscreen) {
					if (Settings.Fullscreen) {
						Engine.Instance.SetFullscreen();
					} else {
						Engine.Instance.SetWindowed(Display.Width * 3, Display.Height * 3);
					}
				}
			}

			if (!tipLabel.Visible && logoCard.DoneLerping && !added) {
				tipLabel.Visible = true;
			}

			if (added && tipLabel.Visible && exitTweenDone) {
				tipLabel.Visible = false;
			}
			
			if (!added && ready && lastV >= 0.99f) {
				added = true;
				logoCard.GoAway = true;

				/*Timer.Add(() => {
					Ui.Add(cards[0] = new PhotoCard {
						Region = new TextureRegion(Textures.FastLoad("Content/egor.png")),
						Name = "Egor Dorichev",
						Tasks = "Code, Art, Sfx & Design",
						Url = "https://twitter.com/egordorichev",
						Position = new Vector2(-350, 135),
						Target = new Vector2(80, 135),
						Angle = 1f
					});
			
					Ui.Add(cards[1] = new PhotoCard {
						Region = new TextureRegion(Textures.FastLoad("Content/jose.png")),
						Name = "Jose Ramon",
						Tasks = "Music & Sfx",
						Url = "https://twitter.com/BibikiGL",
						Position = new Vector2(240, 360),
						Target = new Vector2(240, 135)
					});
			
					cards[1].Start.Y = -160;
				
					Ui.Add(cards[2] = new PhotoCard {
						Region = new TextureRegion(Textures.FastLoad("Content/mate.png")),
						Name = "Mate Cziner",
						Tasks = "Art & Design",
						Url = "https://twitter.com/MateCziner",
						Position = new Vector2(Display.UiWidth + 350, 135),
						Target = new Vector2(400, 135),
						Angle = -1f
					});
				}, 0.3f);*/
			}

			if (ready && added && logoCard.Done && !removed) {
				removed = true;
				Engine.Instance.StateRenderer.UiEffect = Shaders.Ui;
				Engine.Instance.SetState(new InGameState(gameArea, true));
			}
		}

		public override void RenderUi() {
			base.RenderUi();
			
			var v = Math.Min(1, progress / 22f);
			lastV += (v - lastV) * Engine.Delta;

			var w = Display.UiWidth * 0.5f;
			var h = 16;
			var vl = 0.9f;
			var pos = new Vector2((Display.UiWidth - w) / 2, Display.UiHeight * 0.5f + 96);
			var loadingAlpha = Math.Max(0, 1f - (logoCard.Target.Y + 20 - logoCard.Y) / (logoCard.Target.Y + 20));
			
			Graphics.Color = new Color(1, 1, 1, loadingAlpha);
			Graphics.Render(pixel, pos, 0, Vector2.Zero, new Vector2(w, h));
			Graphics.Color = ColorUtils.BlackColor;
			Graphics.Render(pixel, pos + new Vector2(1), 0, Vector2.Zero, new Vector2(w - 2, h - 2));
			Graphics.Color = new Color(vl, vl, vl, loadingAlpha);
			Graphics.Render(pixel, pos + new Vector2(2), 0, Vector2.Zero, new Vector2(lastV * (w - 4), h - 4));
			Graphics.Color = ColorUtils.WhiteColor;

			tipLabel.Tint.A = (byte)(Math.Max(0, 1f - (logoCard.Target.Y - logoCard.Y) / logoCard.Target.Y) * 255);

			Graphics.Color.A = (byte)(loadingAlpha * 255);
			
			var percentage = (int) Math.Round(lastV * 100);

			if (percentage > 5) {
				Graphics.Print($"{percentage}%", Font.Small, (int)(pos.X + lastV * (w - 4)) - 15, (int)pos.Y + 3);
			}

			var loadingLabel = failed ? $"Failed to load: {failure}" : $"Loading: {currentlyLoadingLabel}...";
			Graphics.Print(loadingLabel, Font.Small, Display.UiWidth / 2 - (int)Font.Small.MeasureString(loadingLabel).Width / 2, Display.UiHeight - 20);

			Graphics.Color.A = 255;
		}

		private void GenerateNewTip() {
			exitTweenDone = false;

			// TODO: Increase tip chance when more tips are added.
			var tip = new Random().NextDouble() > 0.2 ? LoadScreenJokes.Generate() : LoadScreenTips.Generate();

			Tween.To(Display.UiWidth + 150, tipLabel.CenterX, x => tipLabel.CenterX = x, 1.5f, Ease.QuadIn).OnEnd += () => {
				tipLabel.Label = tip;
				tipLabel.FinishTyping();
				tipLabel.Center = new Vector2(-150, Display.UiHeight - 55);

				var t = Tween.To(Display.UiWidth / 2f, tipLabel.CenterX, x => tipLabel.CenterX = x, 1.5f, Ease.QuadOut);
				t.OnEnd = () => exitTweenDone = true;
			};
		}

		private void LoadSection(Action section, string name) {
			currentlyLoadingLabel = name;

			var sw = Stopwatch.StartNew();
			
			section();

			progress++;

			if (SectionalLoadTimeLogging) {
				Log.Info($"Loaded section '{name}' in {sw.ElapsedMilliseconds} ms.");
			}
		}
	}
}