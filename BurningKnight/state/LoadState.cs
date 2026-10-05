using System;
using System.Threading;
using BurningKnight.assets;
using BurningKnight.assets.input;
using BurningKnight.assets.lighting;
using BurningKnight.level.biome;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.game;
using Lens.graphics;
using Lens.graphics.animation;
using Lens.input;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.state {
	public partial class LoadState : GameState {
		public string Path = null!;
		private Area gameArea = null!;

		// Set by the loading worker; volatile so the write publishes `gameArea` with it.
		private volatile bool ready;

		// Failure seam, same shape as AssetLoadState's: a worker throw must not abort the
		// process or hang the screen.
		private volatile bool failed;
		private string failure = null!;

		private bool down;
		private float alpha;
		private string title = null!;
		private string prefix = null!;
		private float titleX;
		private float prefixX;
		private float t;

		// Display-only: the progress bar may read a slightly stale value; the hand-off is `ready`.
		private int progress;

		private float timer;
		private bool loading;
		private bool nice;
		public bool IntoCutscene;
		
		public bool Menu;

		private Animation animation = null!;

		// The loading title is drawn with Graphics.Print, which goes straight to
		// DrawString without the markup parser UiString runs. A localised joke that
		// carries `##`/`^^`/`[cl ..]` would otherwise show its raw markers on screen,
		// so they are stripped before the string is measured and drawn. LoadScreenTips
		// goes through a UiString, so its colours still render.
		//
		// The token list mirrors every branch of the parser switch in
		// UiString.Recalculate.cs: bracket tokens (cl, dl, ic, vr, rn, sp, skp, ev,
		// /cl) and the paired markers ^ * % & @ # ~ _ . A bracket run is matched as a
		// whole rather than per known name, so an unknown token is stripped too (the
		// parser would drop it as well) - but only when it really is a token, which
		// is what the leading `\[` escape check is for.
		private static readonly System.Text.RegularExpressions.Regex markupMarkers =
			new System.Text.RegularExpressions.Regex(
				@"(?<!\\)\[[a-z/]+[^\]]*\]|\*\*|\^\^|@@|%%|##|~~|&&|(?<!\\)_");

		private static string Plain(string value) {
			return markupMarkers.Replace(value, string.Empty).Replace("\\[", "[");
		}
		
		
		
		public override void Init() {
			base.Init();
			
			Shaders.Ui.Parameters["black"].SetValue(1f);
				
			animation = Animations.Create("loading");
			animation.Paused = false;

			nice = Rnd.Chance(4.2f);
			
			if (SaveManager.ExistsAndValid(SaveType.Game)
			    && SaveManager.ExistsAndValid(SaveType.Level)
			    && SaveManager.ExistsAndValid(SaveType.Player)) {

				loading = true;
			}

			prefix = Locale.Get(loading || Context.Run.Depth < 1 ? "loading" : "generating");
			title = Plain(new Random().NextDouble() > 0.3 ? LoadScreenJokes.Generate() : BiomeTitles.Generate(BiomeRegistry.GenerateForDepth(Context.Run.Depth).Id));
			
			Lights.Init();
			Physics.Init();
			gameArea = new GameArea();

			Context.Level = null;
			progress = 0;

			var thread = new Thread(() => {
				try {
					Tilesets.Load();

					SaveManager.Load(gameArea, SaveType.Game, Path);
					progress++;

					SaveManager.Load(gameArea, SaveType.Level, Path);
					progress++;

					Context.Run.Luck = 0;
					Context.Run.ResetScourge();

					if (Context.Run.Depth > 0) {
						SaveManager.Load(gameArea, SaveType.Player, Path);
					} else {
						SaveManager.Generate(gameArea, SaveType.Player);
					}

					GC.Collect();
					progress++;
					Engine.AssetsLoaded?.Invoke();
					ready = true;
				} catch (Exception e) {
					// Record for the game thread; an unhandled throw here would abort the process.
					Log.Error("Level loading failed");
					Log.Error(e);

					failure = e.Message.Length > 100 ? $"{e.Message[..100]}…" : e.Message;
					failed = true;
				}
			});

			// A scheduling hint so the loading screen keeps animating; the hand-off is `ready`.
			thread.Priority = ThreadPriority.Lowest;
			thread.IsBackground = true;
			thread.Start();

			titleX = Font.Small.MeasureString(title).Width * -0.5f;
		}

		public override void Destroy() {
			base.Destroy();
			Shaders.Ui.Parameters["black"].SetValue(0f);
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (failed) {
				// Failure is up; any confirm/back press leaves instead of sitting on a dead screen.
				// UiSelect: keyboard/pad, UiAccept: mouse, GameStart/UiBack: pad Start/Back and Esc.
				if (Input.WasPressed(Controls.GameStart) || Input.WasPressed(Controls.UiSelect) || Input.WasPressed(Controls.UiAccept) || Input.WasPressed(Controls.UiBack)) {
					Engine.Instance.Quit();
				}

				return;
			}

			animation.Update(dt);

			t += dt;
			
			timer += dt / 3;
			timer = Math.Min(timer, (progress + 1) * 0.345f);
			
			if (down) {
				if (ready && ((Engine.Version.Dev || loading || Context.Run.Depth == 0) || timer >= 1f)) {
					timer = 1;
					alpha -= dt * 5;
				}
			} else {
				alpha = Math.Min(1, alpha + dt * 5);

				if (alpha >= 0.95f) {
					alpha = 1;
					down = true;
				}
			}

			if (ready && ((down && alpha < 0.05f) || (Engine.Version.Dev) || Context.Run.Depth == 0)) {
				if (IntoCutscene) {
					Engine.Instance.SetState(new CutsceneState(gameArea));
				} else {
					Engine.Instance.SetState(new InGameState(gameArea, Menu));
				}
				
				Menu = false;
			}
		}

		public override void RenderUi() {
			base.RenderUi();

			if (failed) {
				var label = $"Failed to load: {failure}";

				Graphics.Print(label, Font.Small, Display.UiWidth / 2 - (int) Font.Small.MeasureString(label).Width / 2, Display.UiHeight - 20);
				Graphics.Print("Press any button to exit.", Font.Small, Display.UiWidth / 2 - (int) Font.Small.MeasureString("Press any button to exit.").Width / 2, Display.UiHeight - 8);

				return;
			}

			var value = (int) Math.Min(102, Math.Floor(timer * 100f));
			var s = $"{prefix} {(nice && value == 69 ? "Nice." : $"{value}")}%";
			
			prefixX = Font.Medium.MeasureString(s).Width * -0.5f;
			
			Graphics.Color = new Color(1f, 1f, 1f, alpha);
			Graphics.Print(s, Font.Medium, new Vector2(Display.UiWidth / 2f + prefixX, Display.UiHeight / 2f - 8));
			Graphics.Print(title, Font.Small, new Vector2(Display.UiWidth / 2f + titleX, Display.UiHeight / 2f + 8));

			animation.Render(new Vector2(Display.UiWidth / 2f - 5.5f, Display.UiHeight / 2f + 24));
			Graphics.Color = ColorUtils.WhiteColor;
		}

	}
}