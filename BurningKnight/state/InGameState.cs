using System;
using System.Collections.Generic;
using System.Linq;
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
		public static Action<UiTable, string, string, int, Action> SetupLeaderboard = null!;
		public static bool IgnoreSave;
		public static Action SyncAchievements = null!;
		public static bool Multiplayer;
		
		private const float AutoSaveInterval = 60f;
		private const float PaneTransitionTime = 0.2f;
		private const float BarsSize = 50;
		private static readonly float TitleY = BarsSize / 2f;
		private static readonly float BackY = Display.UiHeight - BarsSize / 2f;

		private float blur;
		private static TextureRegion fog = null!;
		
		private UiPane pauseMenu = null!;
		private UiPane leaderMenu = null!;
		private UiPane statsMenu = null!;
		private UiPane gameOverMenu = null!;
		private UiPane credits = null!;

		private UiPane audioSettings = null!;
		private UiPane graphicsSettings = null!;
		private UiPane gameSettings = null!;
		private UiPane confirmationPane = null!;
		private UiPane inputSettings = null!;
		private UiPane gamepadSettings = null!;
		private UiPane keyboardSettings = null!;
		private UiPane languageSettings = null!;
		private UiPane inventory = null!;
		private UiLabel killedLabel = null!;
		private UiLabel placeLabel = null!;

		public bool Died;
		private float saveTimer;
		private SaveIndicator indicator = null!;

		private Painting? painting;

		// The dev-tool hooks; implemented in InGameState.Debug.cs, which a release build
		// excludes (ADR-0003).
		partial void UpdateDebug(float dt);
		partial void FocusAreaDebug(Entity entity);
		partial void UpdateConsole(float dt);
		partial void RenderEditorInGame();
		partial void CreateEditor(Camera camera);
		partial void CreateConsole();

		public bool Menu;
		public Area TopUi = null!;
		
		private float offset;
		private bool menuExited;
		private float blackBarsSize;
		private bool doneAnimatingPause = true;
		
		private TextureRegion gardient = null!;
		private TextureRegion black = null!;
		private TextureRegion emerald = null!;

		public UiAnimation Killer = null!;
		private UiLabel seedLabel = null!;
		private UiButton currentBack = null!;
		private UiButton inputBack = null!;
		private UiButton gamepadBack = null!;
		private UiButton keyboardBack = null!;
		private UiButton languageBack = null!;

		private float timeWas;
		private double startTime;

		public static bool Ready;
		public static bool InMenu;
		
		private static Audio Audio => Context.Audio;

		public static void TransitionToBlack(Vector2 position, Action? callback = null) {
			Context.Camera!.Targets.Clear();
			var v = Context.Camera!.CameraToScreen(position);

			Shaders.Ui.Parameters["bx"].SetValue(v.X / Display.UiWidth);
			Shaders.Ui.Parameters["by"].SetValue(v.Y / Display.UiHeight);

			Tween.To(0, 1, x => Shaders.Ui.Parameters["black"].SetValue(x), 0.7f).OnEnd = callback;

			Audio.FadeOut();
			Ready = false;
		}

		public static void TransitionToOpen(Action? callback = null) {
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

		private void CaptureTime() {
			timeWas = Context.Run.Time;
			startTime = Engine.GameTime.TotalGameTime.TotalSeconds;
		}

		private const float CursorPriority = 0.5f;

		private float speedBeforePause;
		public bool InStats;

		public void OpenBlackBars() {
			Tween.To(BarsSize, blackBarsSize, x => blackBarsSize = x, 0.3f);
		}
		
		public void CloseBlackBars() {
			Tween.To(0, blackBarsSize, x => blackBarsSize = x, 0.2f);
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
			UiButton? btn = null;

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
		private SoundEffectInstance? rainSound;
		private List<Entity> particles = new List<Entity>();

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

		private bool doCheck;

		private void TeleportTo(RoomType type) {
			var player = LocalPlayer.Locate(Area);
			var room = player.GetComponent<RoomComponent>()!.Room;

			foreach (var r in Area.Tagged[Tags.Room].Where(r => r != room && ((Room) r).Type == type))
			{
				player.Center = r.Center;
				return;
			}
		}

		public static bool ToolsEnabled = Engine.Version.Dev;
		
		public static void RenderFog() {
			var shader = Shaders.Fog;
			Shaders.Begin(shader);

			var wind = WindFx.CalculateWind();
			
			shader.Parameters["time"].SetValue(Engine.Time * 0.01f);
			shader.Parameters["tx"].SetValue(wind.X * -0.1f);
			shader.Parameters["ty"].SetValue(wind.Y * -0.1f);
			shader.Parameters["cx"].SetValue(Context.Camera!.Position.X / 512f);
			shader.Parameters["cy"].SetValue(Context.Camera!.Position.Y / 512f);
		
			Graphics.Render(fog, Context.Camera!.TopLeft);
			
			Shaders.End();
		}
		
		private float emeraldY = -20;
		
		private string GetRunTime() {
			var t = Context.Run.Time;
			return $"{(Math.Floor(t / 3600f) + "").PadLeft(2, '0')}:{(Math.Floor(t / 60f % 60f) + "").PadLeft(2, '0')}:{(Math.Floor(t % 60f) + "").PadLeft(2, '0')}";
		}

		private UiLabel loading = null!;
		private UiChoice choice = null!;
		private UiTable leaderStats = null!;
		private UiTable statsStats = null!;
		private Action<string> d = null!;
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
			Context.Run.CalculateScore();
			return $"{Context.Run.Score}".PadLeft(7, '0');
		}

		public Action OnPauseCallback = null!;
		private UiMap map = null!;
		private UiLabel scoreLabel = null!;
		private UiLabel boardType = null!;

		private UiLabel lastCreditsLabel = null!;

		private UiButton pauseBack = null!;
		private UiButton settingsBack = null!;
		private UiButton audioBack = null!;
		private UiButton graphicsBack = null!;
		private UiButton gameBack = null!;
		private UiButton overBack = null!;
		private UiButton overQuickBack = null!;
		private UiButton leaderBack = null!;
		private UiButton inventoryBack = null!;
		private UiButton statsBack = null!;

		public void UpdateRainVolume() {
			if (rainSound != null) {
				rainSound.Volume = (Player.InBuilding ? 0.1f : 0.5f) * Settings.MusicVolume * Settings.MasterVolume;
			}

			Context.Level!.UpdateRainVolume();
		}

		private static readonly string[] Languages =
		[
			"en", "ru", "de", "fr", "pl", "by", "it", "pt", "cn", "ua",
		];
		
		public Action? ReturnFromLeaderboard;
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

		public Action? ReturnFromStats;
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

		public static bool EveryoneDied(Player? pl = null) {
			foreach (var p in Context.Area!.Tagged[Tags.Player]) {
				if (!((Player) p).Dead && p != pl) {
					return false;
				}
			}

			return true;
		}

		public void HandleDeath() {
			Died = true;

			// Synchronous: a handful of small files, and a worker would race the teardown.
			// SaveManager.Save(Area, SaveType.Statistics);
			SaveManager.Delete(SaveType.Player, SaveType.Level, SaveType.Game);
			SaveManager.Backup();
		}

		// private TweenTask last;
	}
}
