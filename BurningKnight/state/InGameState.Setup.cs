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
	public partial class InGameState {
		public override void Init() {
			base.Init();

			if (Context.Run.Depth < 1) {
				Context.Run.Time = 0;
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

			if (Context.Level?.Biome is CastleBiome) {
				for (var i = 0; i < 30; i++) {
					Area.Add(new WindFx());
				}
			}

			fog = Textures.Get("noise");
			Area.Add(new InGameAudio());

			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p is LocalPlayer) {
					bool imp = p.GetComponent<InputComponent>()!.Index == 0;
					Context.Camera.Follow(p, imp ? 1f : 0.5f, imp);
					
					if (imp && Assets.ImGuiEnabled) {
						FocusAreaDebug(p);
					}
				}

				((Player) p).FindSpawnPoint();
			}

			if (!Menu) {
				foreach (var e in TopUi.Tagged[Tags.Cursor]) {
					Context.Camera.Follow(e, CursorPriority);
				}
			}

			Context.Camera.Jump();
			
			if (Context.Run.Depth == 0) {
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
			Context.Run.StartedNew = false;
			
			if (Context.Run.Depth > 0 && GameSave.IsFalse($"reached_{Context.Run.Depth}")) {
				GameSave.Put($"reached_{Context.Run.Depth}", true);
				Area.EventListener.Handle(new NewFloorEvent {
					WasInEL = true
				});
			}
			
			Context.Level.Prepare();

			if (Context.Run.Depth < 1) {
				Scourge.Clear();
			}

			if (Context.Run.Depth == 1 && Area.Tagged[Tags.BurningKnight].Count == 0) {
				Area.Add(new entity.creature.bk.BurningKnight());
			}

			if (Context.Run.Depth == 0) {
				SyncAchievements?.Invoke();
			}

			CaptureTime();
		}
		public void ResetFollowing() {
			Context.Camera.Targets.Clear();
			Context.Camera.MainTarget = null;

			var min = 16;
			
			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p is LocalPlayer lp && !lp.Dead) {
					var index = p.GetComponent<InputComponent>()!.Index;

					if (index < min) {
						min = index;
					}
				}
			}
			
			foreach (var p in Area.Tagged[Tags.Player]) {
				if (p is LocalPlayer) {
					var imp = !Multiplayer || p.GetComponent<InputComponent>()!.Index == min;
					Context.Camera.Follow(p, imp ? 1f : 0.5f, imp);
				}
			}

			if (!Menu) {
				foreach (var e in TopUi.Tagged[Tags.Cursor]) {
					Context.Camera.Follow(e, CursorPriority);
				}
			}
		}
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
					particles.Add(Context.Level.Area.Add(new RainParticle {
						Custom = true
					}));
				}
			} else if (Weather.Snows) {
				for (var i = 0; i < 100; i++) {
					particles.Add(Context.Level.Area.Add(new SnowParticle {
						Custom = true
					}));
				}
			}
		}
		private void PrerenderShadows() {
			var renderer = Engine.Instance.StateRenderer;
			
			renderer.End();
			
			var c = Context.Camera;
			var z = c.Zoom;
			var n = Math.Abs(z - 1) > 0.01f;
				
			if (n) {
				c.Zoom = 1;
				c.UpdateMatrices();
			}
			
			renderer.BeginShadows();

			foreach (var e in Area.Tagged[Tags.HasShadow]) {
				if (!e.Done && (e.AlwaysVisible || e.OnScreen)) {
					e.GetComponent<ShadowComponent>()!.Callback();
				}
			}
			
			renderer.EndShadows();

			if (n) {
				c.Zoom = z;
				c.UpdateMatrices();
			}
			
			renderer.Begin();
		}
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
	}
}
