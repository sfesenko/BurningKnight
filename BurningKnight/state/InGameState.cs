using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BurningKnight.assets;
using BurningKnight.assets.achievements;
using BurningKnight.assets.input;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.npc;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.fx;
using BurningKnight.entity.item;
using BurningKnight.entity.item.use;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.paintings;
using BurningKnight.level.rooms;
using BurningKnight.level.tile;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.ui;
using BurningKnight.ui.dialog;
using BurningKnight.ui.inventory;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.game;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.input;
using Lens.lightJson;
using Lens.util;
using Lens.util.camera;
using Lens.services;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Timer = Lens.util.timer.Timer;

namespace BurningKnight.state {
	public partial class InGameState : GameState, Subscriber {
		public static bool ShouldHide => Engine.Instance.State is InGameState { Paused: true, InStats: false } st && st.currentBack != st.graphicsBack;
		
		public static bool SkipPause;
		public static Action<UiTable, string, string, int, Action> SetupLeaderboard;
		public static bool IgnoreSave;
		public static Action SyncAchievements;
		public static bool Multiplayer;
		
		private const float AutoSaveInterval = 60f;
		private const float PaneTransitionTime = 0.2f;
		private const float BarsSize = 50;
		private static float TitleY = BarsSize / 2f;
		private static float BackY = Display.UiHeight - BarsSize / 2f;

		private float blur;
		private static TextureRegion fog;
		
		private UiPane pauseMenu;
		private UiPane leaderMenu;
		private UiPane statsMenu;
		private UiPane gameOverMenu;
		private UiPane credits;

		private UiPane audioSettings;
		private UiPane graphicsSettings;
		private UiPane gameSettings;
		private UiPane confirmationPane;
		private UiPane inputSettings;
		private UiPane gamepadSettings;
		private UiPane keyboardSettings;
		private UiPane languageSettings;
		private UiPane inventory;
		private UiLabel killedLabel;
		private UiLabel placeLabel;

		public bool Died;
		private float saveTimer;
		private SaveIndicator indicator;
		private SaveLock saveLock = new();

		private Painting painting;

		// The dev-tool hooks; implemented in InGameState.Debug.cs, which a release build
		// excludes (ADR-0003).
		partial void UpdateDebug(float dt);
		partial void FocusAreaDebug(Entity entity);
		partial void UpdateConsole(float dt);
		partial void RenderEditorInGame();
		partial void CreateEditor(Camera camera);
		partial void CreateConsole();

		public bool Menu;
		public Area TopUi;
		
		private float offset;
		private bool menuExited;
		private float blackBarsSize;
		private bool doneAnimatingPause = true;
		
		private TextureRegion gardient;
		private TextureRegion black;
		private TextureRegion emerald;

		public UiAnimation Killer;
		private UiLabel seedLabel;
		private UiButton currentBack;
		private UiButton inputBack;
		private UiButton gamepadBack;
		private UiButton keyboardBack;
		private UiButton languageBack;

		private float timeWas;
		private double startTime;

		public static bool Ready;
		public static bool InMenu;
		
		private static Audio Audio => Audio.Instance;

		public static void TransitionToBlack(Vector2 position, Action callback = null) {
			Camera.Instance.Targets.Clear();
			var v = Camera.Instance.CameraToScreen(position);

			Shaders.Ui.Parameters["bx"].SetValue(v.X / Display.UiWidth);
			Shaders.Ui.Parameters["by"].SetValue(v.Y / Display.UiHeight);

			Tween.To(0, 1, x => Shaders.Ui.Parameters["black"].SetValue(x), 0.7f).OnEnd = callback;

			Audio.FadeOut();
			Ready = false;
		}

		public static void TransitionToOpen(Action callback = null) {
			Shaders.Ui.Parameters["bx"].SetValue(0.333f);
			Shaders.Ui.Parameters["by"].SetValue(0.333f);

			Tween.To(1, 0, x => Shaders.Ui.Parameters["black"].SetValue(x), 0.7f, Ease.QuadIn).OnEnd = () => {
				Ready = true;
				callback?.Invoke();
			};
		}

		public Painting CurrentPainting {
			set {
				painting = value;
				Paused = painting != null;
			}

			get => painting;
		}

		public InGameState(Area area, bool menu) {
			Multiplayer = area.Tagged[Tags.Player].Count > 1;
			
			Menu = menu;
			InMenu = menu;
			Ready = false;
			Input.EnableImGuiFocus = false;

			Area = area;
			Area.EventListener.Subscribe<ItemCheckEvent>(this);
			Area.EventListener.Subscribe<DiedEvent>(this);
			Area.EventListener.Subscribe<GiveEmeraldsUse.GaveEvent>(this);

			black = CommonAse.Ui.GetSlice("black");
			emerald = CommonAse.Items.GetSlice("bk:emerald");

			if (Menu) {
				Achievements.PostLoadCallback?.Invoke();
				Achievements.PostLoadCallback = null;
			
				Input.Blocked = 1;

				blackBarsSize = BarsSize;
				gardient = CommonAse.Ui.GetSlice("gardient");
				blur = 1;

				offset = Display.UiHeight;
				Mouse.SetPosition((int) BK.Instance.GetScreenWidth() / 2, (int) BK.Instance.GetScreenHeight() / 2);

				Timer.Add(() => {
					Tween.To(0, offset, x => offset = x, 2f, Ease.BackOut);
					Audio.PlayMusic("Menu", true);
				}, 1f);
			} else {
				offset = Display.UiHeight;
			}
			
			Shaders.Screen.Parameters["vignette"].SetValue(Settings.Vignette);
		}

		public override void Init() {
			base.Init();

			if (Run.Depth < 1) {
				Run.Time = 0;
			}

			unlockedHat = GlobalSave.IsTrue("bk:fez");

			TopUi = new Area();
			Input.Blocked = 0;

			Audio.Speed = 1f;

			Engine.Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
			Engine.Graphics.ApplyChanges();

			Engine.Instance.StateRenderer.UiEffect = Shaders.Ui;
			
			if (Settings.Fullscreen && !Engine.Graphics.IsFullScreen) {
				Engine.Instance.SetFullscreen();
			}

			Shaders.Ui.Parameters["black"].SetValue(Menu ? 1f : 0f);
			
			SetupUi();

			if (Run.Level?.Biome is CastleBiome) {
				for (var i = 0; i < 30; i++) {
					Area.Add(new WindFx());
				}
			}

			fog = Textures.Get("noise");
			Area.Add(new InGameAudio());

			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p is LocalPlayer) {
					bool imp = p.GetComponent<InputComponent>().Index == 0;
					Camera.Instance.Follow(p, imp ? 1f : 0.5f, imp);
					
					if (imp && Assets.ImGuiEnabled) {
						FocusAreaDebug(p);
					}
				}

				((Player) p).FindSpawnPoint();
			}

			if (!Menu) {
				foreach (var e in TopUi.Tagged[Tags.Cursor]) {
					Camera.Instance.Follow(e, CursorPriority);
				}
			}

			Camera.Instance.Jump();
			
			if (Run.Depth == 0) {
				if (Events.Halloween) {
					Weather.IsNight = true;
				}
				
				if (Weather.IsNight) {
					wasNight = true;
					var x = 0.25f;
					Lights.ClearColor = new Color(x, x, x, 1f);
				}

				if (Weather.Rains || Weather.Snows) {
					SetupParticles();
				}
			}

			if (!Menu) {
				TransitionToOpen();
			}

			FireParticle.Hook(Area);
			Run.StartedNew = false;
			
			if (Run.Depth > 0 && GameSave.IsFalse($"reached_{Run.Depth}")) {
				GameSave.Put($"reached_{Run.Depth}", true);
				Area.EventListener.Handle(new NewFloorEvent {
					WasInEL = true
				});
			}
			
			Run.Level.Prepare();

			if (Run.Depth < 1) {
				Scourge.Clear();
			}

			if (Run.Depth == 1 && Area.Tagged[Tags.BurningKnight].Count == 0) {
				Area.Add(new entity.creature.bk.BurningKnight());
			}

			if (Run.Depth == 0) {
				SyncAchievements?.Invoke();
			}

			CaptureTime();
		}

		private void CaptureTime() {
			timeWas = Run.Time;
			startTime = Engine.GameTime.TotalGameTime.TotalSeconds;
		}

		private const float CursorPriority = 0.5f;

		public void ResetFollowing() {
			Camera.Instance.Targets.Clear();
			Camera.Instance.MainTarget = null;

			var min = 16;
			
			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p is LocalPlayer lp && !lp.Dead) {
					var index = p.GetComponent<InputComponent>().Index;

					if (index < min) {
						min = index;
					}
				}
			}
			
			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p is LocalPlayer) {
					var imp = !Multiplayer || p.GetComponent<InputComponent>().Index == min;
					Camera.Instance.Follow(p, imp ? 1f : 0.5f, imp);
				}
			}

			if (!Menu) {
				foreach (var e in TopUi.Tagged[Tags.Cursor]) {
					Camera.Instance.Follow(e, CursorPriority);
				}
			}
		}

		public override void Destroy() {
			if (Engine.Quiting) {
				Run.SavingDepth = Run.Depth;
			}
			Item.Attact = false;

			if (rainSound != null) {
				if (Engine.Quiting) {
					rainSound.Dispose();
					rainSound = null;
				} else {
					var ss = rainSound;
					rainSound = null;
					Tween.To(0, ss.Volume, x => ss.Volume = x, 0.5f).OnEnd = () => { ss.Dispose(); };
				}
			}
			
			TopUi.Destroy();
			
			Timer.Clear();
			Lights.Destroy();

			Tween.To(1f, Audio.Speed, x => Audio.Speed = x, 1f);
			
			SaveManager.Backup();

			var old = !Engine.Quiting;

			SaveManager.Save(Area, SaveType.Global, old);
			// SaveManager.Save(Area, SaveType.Secret);

			if (!Run.StartedNew && !Died && !Run.Won) {
				var d = (old ? Run.LastDepth : Run.Depth);
				
				if (d > 0) {
					if (IgnoreSave) {
						
					} else {
						SaveManager.Save(Area, SaveType.Level, old);
					}

					IgnoreSave = false;

					SaveManager.Save(Area, SaveType.Player, old);
					SaveManager.Save(Area, SaveType.Game, old);
				}
			}

			Shaders.Screen.Parameters["split"].SetValue(0f);
			Shaders.Screen.Parameters["blur"].SetValue(0f);

			Area.Destroy();
			Area = null;

			Physics.Destroy();
			base.Destroy();
		}

		private float speedBeforePause;
		public bool InStats;

		protected override void OnPause() {
			base.OnPause();
			
			if (Died || InMenu || Run.Won) {
				return;
			}

			t = 0;
			Tween.To(this, new {blur = 1}, 0.25f);

			if (!InStats) {
				currentBack = pauseBack;
				
				if (seedLabel != null) {
					seedLabel.Label = $"{Locale.Get("seed")}: {Run.Seed}";
				}

				if (scoreLabel != null) {
					scoreLabel.Label = GetScore();
				}

				if (Settings.UiSfx) {
					Audio.PlaySfx("ui_goback", 0.5f);
				}

				if (painting == null) {
					doneAnimatingPause = false;

					pauseMenu.X = 0;
					pauseMenu.Enabled = true;
					currentBack = pauseBack;

					Tween.To(0, pauseMenu.Y, x => pauseMenu.Y = x, 0.5f, Ease.BackOut).OnEnd = () => {
						SelectFirst();
						OnPauseCallback?.Invoke();
						OnPauseCallback = null;
					};
				}
			} else {
				currentBack = leaderBack;
			}

			speedBeforePause = Audio.Speed;

			Tween.To(0.5f, Audio.Speed, x => Audio.Speed = x, 1f).OnEnd = () => {
				doneAnimatingPause = true;
			};
			OpenBlackBars();
		}

		public void OpenBlackBars() {
			Tween.To(BarsSize, blackBarsSize, x => blackBarsSize = x, 0.3f);
		}
		
		public void CloseBlackBars() {
			Tween.To(0, blackBarsSize, x => blackBarsSize = x, 0.2f);
		}

		protected override void OnResume() {
			if (painting != null) {
				return;
			}

			base.OnResume();

			if (Died || InMenu || Run.Won) {
				return;
			}

			doneAnimatingPause = false;

			Tween.To(this, new {blur = 0}, 0.25f);

			if (!InStats) {
				Tween.To(-Display.UiHeight, pauseMenu.Y, x => pauseMenu.Y = x, 0.25f).OnEnd = () => {
					pauseMenu.Enabled = false;
				};
			}

			CloseBlackBars();
			Tween.To(speedBeforePause, Audio.Speed, x => Audio.Speed = x, 0.4f);

			Timer.Add(() => {
				doneAnimatingPause = true;
			}, 0.25f);
		}

		public override void OnDeactivated() {
			base.OnDeactivated();

			if (Menu || Paused || DialogComponent.Talking != null || !Settings.Autopause || !menuExited) {
				return;
			}

			Paused = true;
		}

		private float t;
		
		private void SelectFirst() {
			SelectFirst(false);
		}

		private void SelectFirst(bool force) {
			if (!force && GamepadComponent.Current == null) {
				return;
			}
		
			var min = UiButton.LastId;
			UiButton btn = null;

			foreach (var b in TopUi.Tagged[Tags.Button]) {
				var bt = ((UiButton) b);

				if (bt.Active && bt.IsOnScreen() && bt.Id < min) {
					btn = bt;
					min = bt.Id;
				}
			}

			if (btn != null) {
				UiButton.SelectedInstance = btn;
				UiButton.Selected = btn.Id;
			}
		}

		private bool wasNight;
		private bool wasRaining;
		private SoundEffectInstance rainSound;
		private List<Entity> particles = new List<Entity>();

		private void SetupParticles() {
			if (Settings.LowQuality) {
				return;
			}
		
			if (Weather.Rains && Assets.LoadSfx) {
				var s = Audio.GetSfx("level_rain_jungle");

				if (s != null) {
					rainSound = s.CreateInstance();

					if (rainSound != null) {
						rainSound.Volume = 0;
						rainSound.IsLooped = true;
						rainSound.Play();

						Tween.To(0.5f * Settings.MusicVolume * Settings.MasterVolume, 0, x => rainSound.Volume = x, 5f);
					}
				}
				
				for (var i = 0; i < 40; i++) {
					particles.Add(Run.Level.Area.Add(new RainParticle {
						Custom = true
					}));
				}
			} else if (Weather.Snows) {
				for (var i = 0; i < 100; i++) {
					particles.Add(Run.Level.Area.Add(new SnowParticle {
						Custom = true
					}));
				}
			}
		}

		private bool stopped;

		private bool unlockedHat;
		private int comboScore;
		private string[] combo = {
			Controls.UiLeft, Controls.UiDown, Controls.UiRight, Controls.UiUp, 
			Controls.UiDown, Controls.UiRight, Controls.UiDown, Controls.UiLeft
		};

		private string[] possibleButtons = {
			Controls.UiDown, Controls.UiRight, Controls.UiLeft, Controls.UiUp
		};

		private void CheckCombo() {
			var reset = false;
			var data = GamepadComponent.Current;
			
			if (Input.Keyboard.State.GetPressedKeys().Length > 0) {
				reset = true;
			} else if (data != null && data.AnythingIsDown()) {
				reset = true;
			}

			if (!reset) {
				return;
			}

			var found = false;
				
			foreach (var b in possibleButtons) {
				if (Input.WasPressed(b, data)) {
					found = true;
					break;
				}
			}

			if (!found) {
				return;
			}

			var cr = combo[comboScore];

			if (Input.IsDown(cr, data)) {
				foreach (var b in possibleButtons) {
					if (cr != b && Input.IsDown(b, data)) {
						comboScore = 0;
						return;
					}
				}
				
				comboScore++;

				if (comboScore >= 8) {
					Items.Unlock("bk:fez");
					Audio.PlaySfx("level_cleared");
					unlockedHat = true;
				}
			} else {
				comboScore = 0;
			}
		}

		private bool doCheck;

		public override void Update(float dt) {
			if (!unlockedHat) {
				CheckCombo();
			}
			
			if (UiAchievement.Current == null) {
				if (Achievements.AchievementBuffer.Count > 0) {
					var id = Achievements.AchievementBuffer[0];
				
					var a = new UiAchievement(id)
					{
						Y = Display.UiHeight + 60,
						Right = Display.UiWidth - 8
					};
					TopUi.Add(a);
				} else if (Achievements.ItemBuffer.Count > 0) {
					var id = Achievements.ItemBuffer[0];
				
					var a = new UiAchievement(id, true)
					{
						Y = Display.UiHeight + 60,
						Right = Display.UiWidth - 8
					};
					TopUi.Add(a);
				}
			}
			
			if (!Paused && (Settings.Autosave && Run.Depth > 0)) {
				if (!saving) {
					saveTimer += dt;

					if (saveTimer >= AutoSaveInterval) {
						saveTimer = 0;
						saving = true;
						saveLock.Reset();

						indicator.HandleEvent(new SaveStartedEvent());

						new Thread(() => {
							try {
								SaveManager.Backup();

								SaveManager.ThreadSave(saveLock.UnlockGlobal, Area, SaveType.Global);
								SaveManager.ThreadSave(saveLock.UnlockGame, Area, SaveType.Game);

								SaveManager.ThreadSave(saveLock.UnlockLevel, Area, SaveType.Level);
								SaveManager.ThreadSave(saveLock.UnlockPlayer, Area, SaveType.Player);
							} catch (Exception e) {
								Log.Error(e);
							}
						}) {
							Priority = ThreadPriority.Lowest
						}.Start();
					}
				} else if (saveLock.Done) {
					saving = false;
					indicator.HandleEvent(new SaveEndedEvent());
				}
			}

			if (credits is { Enabled: true }) {
				if (lastCreditsLabel.Y <= Display.UiHeight * 0.75f) {
					if (!stopped) {
						stopped = true;
						
						Timer.Add(() => {
							gameSettings.Enabled = true;

							Tween.To(Display.UiWidth * -2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
								credits.Enabled = false;
								SelectFirst();
							};
						}, Input.IsDown(Controls.UiSelect, GamepadComponent.Current) ? 0.1f : 1f);
					}
				} else {
					stopped = false;
					credits.RelativeY -= dt * 30 * (Input.IsDown(Controls.UiSelect, GamepadComponent.Current) ? 6 : 1);
				}
			}
			
			var gamepad = GamepadComponent.Current;

			if (Died && Input.WasPressed(Controls.QuickRestart)) {
				overQuickBack?.OnClick();
			}
			
			if ((Paused || Died || Run.Won) && UiControl.Focused == null) {
				if (UiButton.SelectedInstance != null && (!UiButton.SelectedInstance.Active || !UiButton.SelectedInstance.IsOnScreen())) {
					UiButton.SelectedInstance = null;
					UiButton.Selected = -1;
				}

				var inControl = (currentBack == gamepadBack && UiButton.SelectedInstance is UiControl) || currentBack == keyboardBack;
				
				if (UiButton.SelectedInstance == null && (Input.WasPressed(Controls.UiDown, gamepad, true) || Input.WasPressed(Controls.UiUp, gamepad, true) || (inControl && (Input.WasPressed(Controls.UiLeft, gamepad, true) || Input.WasPressed(Controls.UiRight, gamepad, true))))) {
					SelectFirst(true);

					if (Settings.UiSfx) {
						Audio.PlaySfx("ui_moving", 0.5f);
					}
				} else if (UiButton.Selected > -1) {
					if (Input.WasPressed(Controls.UiDown, gamepad, true) || (inControl && Input.WasPressed(Controls.UiRight, gamepad, true))) {
						UiButton sm = null;
						var mn = UiButton.LastId;
						
						foreach (var b in TopUi.Tagged[Tags.Button]) {
							var bt = (UiButton) b;

							if (bt.Active && bt.IsOnScreen() && bt.Id > UiButton.Selected && bt.Id < mn) {
								mn = bt.Id;
								sm = bt;
							}
						}

						if (sm != null) {
							UiButton.SelectedInstance = sm;
							UiButton.Selected = sm.Id;

							if (Settings.UiSfx) {
								Audio.PlaySfx("ui_moving", 0.5f);
							}
						} else {
							var min = UiButton.Selected;
							UiButton btn = null;
							
							foreach (var b in TopUi.Tagged[Tags.Button]) {
								var bt = ((UiButton) b);

								if (bt.Active && bt.IsOnScreen() && bt.Id < min) {
									btn = bt;
									min = bt.Id;
								}
							}

							if (btn != null) {
								UiButton.SelectedInstance = btn;
								UiButton.Selected = btn.Id;

								if (Settings.UiSfx) {
									Audio.PlaySfx("ui_moving", 0.5f);
								}
							}
						}
					} else if (Input.WasPressed(Controls.UiUp, gamepad, true) || (inControl && Input.WasPressed(Controls.UiLeft, gamepad, true))) {
						UiButton sm = null;
						var mn = -1;
						
						foreach (var b in TopUi.Tagged[Tags.Button]) {
							var bt = ((UiButton) b);

							if (bt.Active && bt.IsOnScreen() && bt.Id < UiButton.Selected && bt.Id > mn) {
								mn = bt.Id;
								sm = bt;
							}
						}

						if (sm != null) {
							UiButton.SelectedInstance = sm;
							UiButton.Selected = sm.Id;

							if (Settings.UiSfx) {
								Audio.PlaySfx("ui_moving", 0.5f);
							}
						} else {
							var max = -1;
							UiButton btn = null;
							
							foreach (var b in TopUi.Tagged[Tags.Button]) {
								var bt = ((UiButton) b);

								if (bt.Active && bt.IsOnScreen() && bt.Id > max) {
									btn = bt;
									max = bt.Id;
								}
							}

							if (btn != null) {
								UiButton.SelectedInstance = btn;
								UiButton.Selected = btn.Id;

								if (Settings.UiSfx) {
									Audio.PlaySfx("ui_moving", 0.5f);
								}
							}
						}
					}
				}
			}

			if (!Paused) {
				t += dt;
				Weather.Update(dt);

				if (Run.Depth == 0) {
					var night = Weather.IsNight || Events.Halloween;

					if (night != wasNight) {
						wasNight = night;
						var v = night ? 0.25f : 0.9f;

						Tween.To(v, Lights.ClearColor.R / 255f, x => { Lights.ClearColor = new Color(x, x, x, 1f); }, 10f);
					}

					var raining = Weather.Rains || Weather.Snows;

					if (wasRaining != raining) {
						wasRaining = raining;

						if (raining) {
							SetupParticles();
						} else {
							foreach (var p in particles) {
								if (p is RainParticle r) {
									r.End = true;
								} else if (p is SnowParticle s) {
									s.End = true;
								} else {
									p.Done = true;
								}
							}

							particles.Clear();

							if (rainSound != null) {
								var ss = rainSound;
								Tween.To(0, ss.Volume, x => ss.Volume = x, 3f);
								rainSound = null;
							}
						}
					}
				}
			}
			
			var inside = Engine.GraphicsDevice.Viewport.Bounds.Contains(Input.Mouse.CurrentState.Position);
			
			Shaders.Screen.Parameters["split"].SetValue(Engine.Instance.Split);
			Shaders.Screen.Parameters["blur"].SetValue(blur);

			if (DialogComponent.Talking == null) {
				if (!Paused && t >= 1f && !inside && Settings.Autopause && !Menu) {
					Paused = true;
				}/* else if (Paused && pausedByMouseOut && inside) {
					Paused = false;
				}*/
			}

			if (Menu && !menuExited) {
				if (Input.WasPressed(Controls.GameStart, GamepadComponent.Current, true)) {
					menuExited = true;
					InMenu = false;
					Input.Blocked = 0;

					Audio.PlaySfx("ui_start");
					Audio.PlayMusic("Hub", true);

					CloseBlackBars();

					Tween.To(this, new {blur = 0}, 0.5f).OnEnd = () => {
						foreach (var e in TopUi.Tagged[Tags.Cursor]) {
							Camera.Instance.Follow(e, CursorPriority);
						}
					};

					Camera.Instance.Detached = false;
					Tween.To(-Display.UiHeight, offset, x => offset = x, 0.5f, Ease.QuadIn).OnEnd = () => {
						Menu = false;

						Timer.Add(() => {
							foreach (var n in Area.Tagged[Tags.Npc]) {
								if (n is OldMan m) {
									m.GetComponent<DialogComponent>().StartAndClose("shopkeeper_6", 3);
									break;
								}
							}
						}, 0.5f);
					};
				}
			}
			
			if (!Paused) {
				if (!Died && !Run.Won && Run.Depth > 0) {
					Run.Time = (float) (timeWas + (Engine.GameTime.TotalGameTime.TotalSeconds - startTime));
				} else {
					CaptureTime();
				}

				var d = PlayerInputComponent.EnableUpdates ? dt : 0;

				Physics.Update(d);
				base.Update(d);
			} else {
				Ui.Update(dt);
			}
			
			UpdateConsole(dt);

			var controller = GamepadComponent.Current;
			
			if (painting != null) {
				if (Input.WasPressed(Controls.Pause, controller) || Input.WasPressed(Controls.Interact, controller) ||
				    Input.WasPressed(Controls.Use, controller)) {
					painting.Remove();
				}
			} else {
				if (doCheck) {
					if (UiControl.Focused != null) {
						UiControl.Focused.DoCheck();

						if (UiControl.Focused != null) {
							UiControl.Focused.Cancel();
						}
					}

					doCheck = false;
				}

				if (doneAnimatingPause) {
					var did = false;

					if (DialogComponent.Talking == null) {
						if (!(animating) && Input.WasPressed(Controls.Pause, controller)) {
							if (SkipPause) {
								SkipPause = false;
							} else if (Paused) {
								if (UiControl.Focused == null && currentBack == null) {
									Paused = false;
									did = true;
								}
							} else if (!Menu) {
								Paused = true;
								did = true;
							}
						}

						if (!did && (Paused || Died || Run.Won) && Input.WasPressed(Controls.UiBack, controller)) {
							if (Settings.UiSfx) {
								Audio.PlaySfx("ui_exit", 0.5f);
							}

							if (UiControl.Focused != null) {
								doCheck = true;
							} else if (currentBack != null) {
								currentBack.Click(currentBack);
							} else {
								Paused = false;
							}
						}
					}
				}
			}

			#if DEBUG
				UpdateDebug(dt);
				Tilesets.Update();
			#endif

			Run.Update();
			
			if (Input.WasPressed(Controls.Fullscreen) || (Input.Keyboard.WasPressed(Keys.Enter) && (Input.Keyboard.IsDown(Keys.LeftAlt) || Input.Keyboard.IsDown(Keys.RightAlt)))) {
				if (Engine.Graphics.IsFullScreen) {
					Engine.Instance.SetWindowed(Display.Width * 3, Display.Height * 3);
				} else {
					Engine.Instance.SetFullscreen();
				}
				
				Settings.Fullscreen = Engine.Graphics.IsFullScreen;
			}

			TopUi.Update(dt);
		}

		private bool saving;
		
		private void TeleportTo(RoomType type) {
			var player = LocalPlayer.Locate(Area);
			var room = player.GetComponent<RoomComponent>().Room;

			foreach (var r in Area.Tagged[Tags.Room].Where(r => r != room && ((Room) r).Type == type))
			{
				player.Center = r.Center;
				return;
			}
		}

		public static bool ToolsEnabled = Engine.Version.Dev;
		
		private void PrerenderShadows() {
			var renderer = (PixelPerfectGameRenderer) Engine.Instance.StateRenderer;
			
			renderer.End();
			
			var c = Camera.Instance;
			var z = c.Zoom;
			var n = Math.Abs(z - 1) > 0.01f;
				
			if (n) {
				c.Zoom = 1;
				c.UpdateMatrices();
			}
			
			renderer.BeginShadows();

			foreach (var e in Area.Tagged[Tags.HasShadow]) {
				if (!e.Done && (e.AlwaysVisible || e.OnScreen)) {
					e.GetComponent<ShadowComponent>().Callback();
				}
			}
			
			renderer.EndShadows();

			if (n) {
				c.Zoom = z;
				c.UpdateMatrices();
			}
			
			renderer.Begin();
		}
		
		public static void RenderFog() {
			var shader = Shaders.Fog;
			Shaders.Begin(shader);

			var wind = WindFx.CalculateWind();
			
			shader.Parameters["time"].SetValue(Engine.Time * 0.01f);
			shader.Parameters["tx"].SetValue(wind.X * -0.1f);
			shader.Parameters["ty"].SetValue(wind.Y * -0.1f);
			shader.Parameters["cx"].SetValue(Camera.Instance.Position.X / 512f);
			shader.Parameters["cy"].SetValue(Camera.Instance.Position.Y / 512f);
		
			Graphics.Render(fog, Camera.Instance.TopLeft);
			
			Shaders.End();
		}
		
		public override void Render() {
			PrerenderShadows();
			base.Render();
			Physics.Render();
			RenderEditorInGame();
			
			if (RenderDebug) {
				Ui?.RenderDebug();
				TopUi?.RenderDebug();
			}
		}

		private float emeraldY = -20;
		
		public override void RenderUi() {
			base.RenderUi();
			
			if (!Settings.HideUi) {
				if (Run.Depth == 0 || emeraldY > -20) {
					var y = Run.Depth == 0 ? 0 : emeraldY;
					var str = $"{GlobalSave.Emeralds}";
					var xx = Display.UiWidth - emerald.Width - 8;

					Graphics.Render(emerald, new Vector2(xx, 8 + y));
					Graphics.Print(str, Font.Small, new Vector2(xx - 8 - Font.Small.MeasureString(str).Width, 9 + y));
				}
			}

			if (blackBarsSize > 0.01f) {
				Graphics.Render(black, Vector2.Zero, 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, blackBarsSize));
				Graphics.Render(black, new Vector2(0, Display.UiHeight + 1 - blackBarsSize), 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, blackBarsSize + 1));
			}

			painting?.RenderUi();

			TopUi.Render();

			if (Settings.HideUi) {
				foreach (var e in TopUi.Tagged[Tags.Cursor]) {
					e.Render();
				}

				return;
			}

			if (Menu && offset <= Display.UiHeight) {
				Graphics.Render(black, new Vector2(0, offset - Display.UiHeight), 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, Display.UiHeight + 1));
				Graphics.Render(gardient, new Vector2(0, offset), 0, Vector2.Zero, new Vector2(Display.UiWidth + 1, (Display.UiHeight + 1) / 90f));
				
				Graphics.Print("Press X", Font.Small, Display.Height + 48 + (int) offset);
				
				LogoRenderer.Render(offset);
			}

			var x = 1;
			
			if (Settings.ShowFps) {
				var c = Engine.Instance.AverageFramesPerSecond;

				var color = c switch
				{
					>= 55 => Color.Green,
					>= 45 => Color.Yellow,
					_ => Color.Red
				};

				Graphics.Color = color;
				var s = $"{c}";
				Graphics.Print(s, Font.Small, x, 1);
				x += (int) Font.Small.MeasureString(s).Width + 1;
				Graphics.Color = ColorUtils.WhiteColor;
			}

			if (Settings.SpeedrunTimer) {
				Graphics.Print(GetRunTime(), Font.Small, x, 1);
			}

			// Graphics.Batch.DrawString(Font.Test, "Test 你 Test", Vector2.One, Color.White);
			
			if (RenderDebug) {
				Ui?.RenderDebug();
				TopUi?.RenderDebug();
			}
		}

		private string GetRunTime() {
			var t = Run.Time;
			return $"{(Math.Floor(t / 3600f) + "").PadLeft(2, '0')}:{(Math.Floor(t / 60f % 60f) + "").PadLeft(2, '0')}:{(Math.Floor(t % 60f) + "").PadLeft(2, '0')}";
		}

		private UiLabel loading;
		private UiChoice choice;
		private UiTable leaderStats;
		private UiTable statsStats;
		private Action<string> d;
		private List<UiItem> inventoryItems = new List<UiItem>();

		public void GoToInventory() {
			currentBack = inventoryBack;
			inventory.Enabled = true;
			SetupInventory();

			Tween.To(-Display.UiHeight, pauseMenu.Y, x => pauseMenu.Y = x, PaneTransitionTime).OnEnd = () => {
				pauseMenu.Enabled = false;
				SelectFirst();
			};
		}

		private string GetScore() {
			Run.CalculateScore();
			return $"{Run.Score}".PadLeft(7, '0');
		}

		public Action OnPauseCallback;
		private UiMap map;
		private UiLabel scoreLabel;
		private UiLabel boardType;

		private void SetupUi() {
			TopUi.Add(new UiChat());
			
			UiButton.LastId = 0;
			
			var cam = new Camera(new FollowingDriver());
			TopUi.Add(cam);
			// Ui.Add(new AchievementBanner());

			CreateEditor(cam);

			var id = Run.Level.Biome.Id;

			if (id != Biome.Castle && id != Biome.Hub) {
				Achievements.Unlock($"bk:{id}");
				var i = Run.Level.Biome.GetItemUnlock();

				if (i != null) {
					Items.Unlock(i);
				}
			}

			foreach (var p in Area.Tagged[Tags.Player]) {
				var cursor = new Cursor {
					Player = (Player) p
				};
				
				TopUi.Add(cursor);
				p.GetComponent<CursorComponent>().Cursor = cursor;
			}
			
			Ui.Add(indicator = new SaveIndicator());

			var player = LocalPlayer.Locate(Area);

			if (!Multiplayer && Run.Depth > 0) {
				Ui.Add(map = new UiMap(player));
			}	
			
			CreateConsole();

			foreach (var p in Area.Tagged[Tags.Player]) {
				Ui.Add(new UiInventory((Player) p, Multiplayer));
			}

			TopUi.Add(pauseMenu = new UiPane {
				Y = -Display.UiHeight	
			});
			
			TopUi.Add(leaderMenu = new UiPane());

			var space = 24f;
			var start = Display.UiHeight * 0.5f + (Run.Depth > 0 ? -space * 0.5f : space);

			pauseMenu.Add(new UiLabel {
				Label = Level.GetDepthString(),
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = TitleY,
				Clickable = false,
				AngleMod = 0
			});
			
			pauseMenu.Add(new UiLabel {
				Font = Font.Small,
				Label = BK.Version.ToString(),
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = TitleY + 12,
				Clickable = false,
				AngleMod = 0
			});

			if (Run.Depth > 0) {
				scoreLabel = (UiLabel) pauseMenu.Add(new UiLabel {
					Font = Font.Small,
					Label = GetScore(),
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = TitleY + 24,
					Clickable = false,
					AngleMod = 0
				});
			}

			if (Run.Depth > 0) {
				pauseMenu.Add(seedLabel = new UiButton {
					Font = Font.Small,
					Selectable = false,
					Label = $"Seed: {Run.Seed}",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = BackY,
					AngleMod = 0,
					Click = b => {
						b.LocaleLabel = "copied_to_clipboard";

						try {
							// Needs xclip on linux
							Clipboard.SetText(Run.Seed);
						} catch (Exception e) {
							Log.Error(e);
						}

						Timer.Add(() => { b.Label = $"{Locale.Get("seed")}: {Run.Seed}"; }, 0.5f);
					}
				});
			}

			pauseBack = currentBack = (UiButton) pauseMenu.Add(new UiButton {
				LocaleLabel = "resume",
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = start - space,
				Click = b => Paused = false
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "settings",
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = start,
				Click = b => {
					currentBack = settingsBack;
					Tween.To(-Display.UiWidth, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = SelectFirst;
				}
			});
			
			if (Run.Depth > 0) {
			
				pauseMenu.Add(new UiButton {
					LocaleLabel = "inventory",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = start + space,
					Click = b => {
						GoToInventory();
					}
				});
			
				pauseMenu.Add(inventory = new UiPane {
					RelativeY = Display.UiHeight
				});

				var sx = Display.UiWidth * 0.5f;

				inventory.Add(new UiLabel {
					LocaleLabel = "inventory",
					RelativeCenterX = sx,
					RelativeCenterY = TitleY,
					Clickable = false
				});
			
				inventoryBack = (UiButton) inventory.Add(new UiButton {
					LocaleLabel = "back",
					Type = ButtonType.Exit,
					RelativeCenterX = sx,
					RelativeCenterY = BackY,
					Click = b => {
						currentBack = pauseBack;
						pauseMenu.Enabled = true;
					
						Tween.To(0, pauseMenu.Y, x => pauseMenu.Y = x, PaneTransitionTime).OnEnd = () => {
							SelectFirst();
							inventory.Enabled = false;

							foreach (var i in inventoryItems) {
								i.Done = true;
							}

							inventoryItems.Clear();
						};
					}
				});
			
				inventory.Enabled = false;
			
				if (Run.Type != RunType.Daily) {
					pauseMenu.Add(new UiButton {
						LocaleLabel = "new_run",
						RelativeCenterX = Display.UiWidth / 2f,
						RelativeCenterY = start + space * 2,
						Type = ButtonType.Exit,
						Click = b => GoConfirm("start_new_run", () => { Run.StartNew(); }, () => {
							currentBack = pauseBack;
							pauseMenu.Enabled = true;

							Tween.To(0, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
								pauseMenu.Remove(confirmationPane);
								confirmationPane = null;
								SelectFirst();
							};
						})
					});
				}
			} else if (Run.Depth == 0) {
				pauseMenu.Add(new UiButton {
					LocaleLabel = "exit",
					Type = ButtonType.Exit,
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = BackY,
					Click = b => {
						Engine.Instance.Exit();
					}
				});
			}

			if (Run.Depth != 0) {
				pauseMenu.Add(new UiButton {
					LocaleLabel = "back_to_town",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = start + space * 3,
					Type = ButtonType.Exit,
					Click = b => Run.Depth = 0
				});
			}

			AddSettings();
			
			pauseMenu.Setup();
			
			TopUi.Add(gameOverMenu = new UiPane {
				Y = -Display.UiHeight
			});
			
			space = 20f;
			start = (Display.UiHeight) / 2f - space;

			killedLabel = (UiLabel) gameOverMenu.Add(new UiLabel {
				Font = Font.Small,
				LocaleLabel = "killed_by",
				RelativeCenterX = Display.UiWidth * 0.75f,
				RelativeCenterY = start - space,
				Tints = false,
				Clickable = false
			});

			Killer = (UiAnimation) gameOverMenu.Add(new UiAnimation {
				RelativeCenterX = Display.UiWidth * 0.75f,
				RelativeY = start,
				Clickable = false
			});

			var qr = Run.Depth > 0 && (Run.Type == RunType.Regular || Run.Type == RunType.Challenge || Run.Type == RunType.BossRush);
			
			if (qr) {
				gameOverMenu.Add(overQuickBack = new UiButton {
					Font = Font.Small,
					LocaleLabel = "quick_restart",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = BackY - 6,

					Click = b => {
						if (Run.Type == RunType.BossRush) {
							if (GlobalSave.Emeralds < 3) {
								AnimationUtil.ActionFailed();
								return;
							}
							
							GlobalSave.Emeralds -= 3;
						}

						gameOverMenu.Enabled = false;
						Run.StartNew(1, Run.Type);
					}
				});
			}

			gameOverMenu.Add(overBack = new UiButton {
				LocaleLabel = "back_to_town",
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = BackY + (qr ? 12 : 0),

				Click = b => {
					gameOverMenu.Enabled = false;
					Run.StartNew(Run.Depth == -2 ? -2 : 0);
				}
			});

			gameOverMenu.Setup();
			gameOverMenu.Enabled = false;

			if (Run.Depth > 0 && Run.Level != null && !Menu) {
				Ui.Add(new UiBanner(Level.GetDepthString()));
			}
			
			leaderMenu.Add(boardType = new UiLabel {
				Label = $"{Locale.Get($"run_{Run.Type.ToString().ToLower()}")} {Locale.Get("leaderboard")}",
				RelativeCenterX = Display.UiWidth * 0.5f,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			leaderStats = new UiTable();
			leaderMenu.Add(leaderStats);

			if (SetupLeaderboard == null) {
				leaderStats.Add("No steam no leaderboards", ":(");
				leaderStats.Prepare();

				leaderStats.RelativeCenterX = Display.UiWidth * 0.5f;
				leaderStats.RelativeCenterY = Display.UiHeight * 0.5f;
			} else {
				loading = (UiLabel) leaderMenu.Add(new UiLabel {
					LocaleLabel = "loading",
					RelativeCenterX = Display.UiWidth * 0.5f,
					RelativeCenterY = Display.UiHeight * 0.5f,
					Clickable = false,
					Hide = true
				});
				
				var offset = 0;
				string lastS = null;
				
				d = (s) =>{
					if (s == null) {
						s = lastS ?? Run.GetLeaderboardId();
					}

					lastS = s;
					
					loading.Hide = false;
					choice.Disabled = true;
					
					leaderStats.Clear();
					offset = Math.Max(0, offset);

					SetupLeaderboard(leaderStats, s, choice.Options[choice.Option], offset, () => {
						leaderStats.Prepare();

						leaderStats.RelativeCenterX = Display.UiWidth * 0.5f;
						leaderStats.RelativeCenterY = Display.UiHeight * 0.5f;
						
						loading.Hide = true;
						choice.Disabled = false;
					});
				};

				leaderMenu.Add(new UiButton {
					Label = "-",
					XPadding = 4,
					Selectable = false,
					RelativeX = Display.UiWidth * 0.5f - 10,
					RelativeCenterY = BackY - 25,
					Type = ButtonType.Slider,
					Click = bt => {
						offset = Math.Max(0, offset - 10);
						d(null);
					},
					ScaleMod = 3
				});
				
				leaderMenu.Add(new UiButton {
					Label = "+",
					XPadding = 4,
					Selectable = false,
					RelativeX = Display.UiWidth * 0.5f + 10,
					RelativeCenterY = BackY - 25,
					Type = ButtonType.Slider,
					Click = bt => {
						offset += 10;
						d(null);
					},
					ScaleMod = 3
				});
				
				leaderMenu.Add(choice = new UiChoice {
					Font = Font.Small,
					Name = "display",
					Options = new [] {
						"global", "around_you", "friends"
					},
				
					Option = 0,
					Click = c => {
						offset = 0;
						d(null);
					},
					RelativeX = Display.UiWidth * 0.5f,
					RelativeCenterY = TitleY + 12
				});
			}
				
			leaderMenu.Add(leaderBack = new UiButton {
				LocaleLabel = "back",
				RelativeCenterX = Display.UiWidth * 0.5f,
				RelativeCenterY = BackY,
				Click = bt => {
					HideLeaderboard();
				},
			});

			leaderMenu.Enabled = false;
			leaderMenu.Y = Display.UiHeight * 2;
			
			if (Run.Depth == 0) {
				TopUi.Add(statsMenu = new UiPane());
				
				placeLabel = (UiLabel) statsMenu.Add(new UiLabel {
					Label = "404",
					RelativeCenterX = Display.UiWidth * 0.5f,
					RelativeCenterY = TitleY,
					Clickable = false
				});
				
				statsMenu.Add(statsBack = new UiButton {
					LocaleLabel = "back",
					RelativeCenterX = Display.UiWidth * 0.5f,
					RelativeCenterY = BackY,
					Click = bt => {
						HideStats();
					},
				});

				statsStats = new UiTable();
				statsMenu.Add(statsStats);

				statsStats.Prepare();

				statsStats.RelativeCenterX = Display.UiWidth * 0.5f;
				statsStats.RelativeCenterY = Display.UiHeight * 0.5f;
				
				statsMenu.Enabled = false;
				statsMenu.Y = Display.UiHeight * 2;
			}
		}

		private UiLabel lastCreditsLabel;

		private void SetupCredits() {
			if (credits != null) {
				credits.RelativeY = 0;
				return;
			}
			
			pauseMenu.Add(credits = new UiPane {
				RelativeX = Display.UiWidth * 3
			});
			
			var y = TitleY + 128;
			var count = Credits.Text.Count;
			lastCreditsLabel = null;
			
			for (var i = 0; i < count; i++) {
				var text = Credits.Text[i];
				
				foreach (var s in text) {
					lastCreditsLabel = (UiLabel) credits.Add(new UiLabel {
						Font = Font.Medium,
						Label = s,
						RelativeCenterX = Display.UiWidth * 0.5f,
						RelativeCenterY = y,
						Tints = false,
						Clickable = false
					});

					y += 12f;
				}

				y += 24f;
			}

			credits.Setup();
			credits.Enabled = false;
		}

		private void SetupInventory() {
			var player = LocalPlayer.Locate(Area);

			if (player == null) {
				return;
			}

			var iv = player.GetComponent<InventoryComponent>();
			var offset = Math.Min(iv.Items.Count, 10) * 24 * 0.5f; 

			for (var i = 0; i < iv.Items.Count; i++) {
				var item = new UiItem();
				var it = iv.Items[i];
				
				item.Id = it.Id;
				item.Scourged = it.Scourged;

				item.RelativeCenterX = Display.UiWidth * 0.5f - offset + i % 10 * 24;
				item.RelativeY = 72 + (float) Math.Floor(i / 10f) * 24;

				inventory.Add(item);
				inventoryItems.Add(item);
			}
		}

		private UiButton pauseBack;
		private UiButton settingsBack;
		private UiButton audioBack;
		private UiButton graphicsBack;
		private UiButton gameBack;
		private UiButton overBack;
		private UiButton overQuickBack;
		private UiButton leaderBack;
		private UiButton inventoryBack;
		private UiButton statsBack;

		public void UpdateRainVolume() {
			if (rainSound != null) {
				rainSound.Volume = (Player.InBuilding ? 0.1f : 0.5f) * Settings.MusicVolume * Settings.MasterVolume;
			}

			Run.Level.UpdateRainVolume();
		}

		private static readonly string[] Languages =
		[
			"en", "ru", "de", "fr", "pl", "by", "it", "pt", "cn", "ua",
		];
		
		public Action ReturnFromLeaderboard;
		private bool busy;

		private void HideLeaderboard() {
			if (!busy || animating) {
				return;
			}

			busy = false;
			animating = true;
			
			Tween.To(Display.UiHeight * 2, leaderMenu.Y, x => leaderMenu.Y = x, 0.6f).OnEnd = () => {
				SelectFirst();			
				leaderMenu.Enabled = false;
				ReturnFromLeaderboard?.Invoke();
				animating = false;
			};
			
			Paused = false;
		}

		public override void OnActivated() {
			base.OnActivated();
			t = 0;
		}

		public Action ReturnFromStats;
		private bool sbusy;

		private bool animating;

		private void HideStats() {
			if (!sbusy || animating) {
				return;
			}

			sbusy = false;
			animating = true;
			
			Tween.To(Display.UiHeight * 2, statsMenu.Y, x => statsMenu.Y = x, 0.6f).OnEnd = () => {
				SelectFirst();			
				statsMenu.Enabled = false;
				ReturnFromStats?.Invoke();
				animating = false;
			};

			Paused = false;
		}

		public static bool EveryoneDied(Player pl = null) {
			foreach (var p in Engine.Instance.State.Area.Tagged[Tags.Player]) {
				if (!((Player) p).Dead && p != pl) {
					return false;
				}
			}

			return true;
		}

		public void HandleDeath() {
			Died = true;

			// Not awaited: this runs while the death screen animates. RunAsync logs any failure.
			_ = AsyncUtils.RunAsync("Death", () =>
			{
				// SaveManager.Save(Area, SaveType.Statistics);
				SaveManager.Delete(SaveType.Player, SaveType.Level, SaveType.Game);
				SaveManager.Backup();
			});
		}

		// private TweenTask last;

		public bool HandleEvent(Event e) {
			if (e is GiveEmeraldsUse.GaveEvent ge) {
				Tween.To(0, emeraldY, x => emeraldY = x, 0.4f, Ease.BackOut).OnEnd = () => {
					// Tween.Remove(last);

					Tween.To(-20, emeraldY, x => emeraldY = x, 0.3f, Ease.QuadIn)
						.Delay = 3;
				};
				
				return false;
			}
			
			if (Died || Run.Won) {
				return false;
			}
			
			if (e is DiedEvent { Who: Mob }) {
				Run.KillCount++;
			}

			return false;
		}
	}
}
