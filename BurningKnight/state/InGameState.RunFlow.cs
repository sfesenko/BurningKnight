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
using System.Text.Json.Nodes;
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
		public override void Destroy() {
			if (Engine.Quiting) {
				Context.Run.SavingDepth = Context.Run.Depth;
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
			
			var old = !Engine.Quiting;

			SaveManager.Save(Area, SaveType.Global, old);
			// SaveManager.Save(Area, SaveType.Secret);

			if (!Context.Run.StartedNew && !Died && !Context.Run.Won) {
				var d = (old ? Context.Run.LastDepth : Context.Run.Depth);
				
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
			Area = null!;

			Physics.Destroy();
			base.Destroy();
		}
		protected override void OnPause() {
			base.OnPause();
			
			if (Died || InMenu || Context.Run.Won) {
				return;
			}

			t = 0;
			Tween.To(this, new {blur = 1}, 0.25f);

			if (!InStats) {
				currentBack = pauseBack;
				
				if (seedLabel != null) {
					seedLabel.Label = $"{Locale.Get("seed")}: {Context.Run.Seed}";
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
		protected override void OnResume() {
			if (painting != null) {
				return;
			}

			base.OnResume();

			if (Died || InMenu || Context.Run.Won) {
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
		public bool HandleEvent(Event e) {
			if (e is GiveEmeraldsUse.GaveEvent ge) {
				Tween.To(0, emeraldY, x => emeraldY = x, 0.4f, Ease.BackOut).OnEnd = () => {
					// Tween.Remove(last);

					Tween.To(-20, emeraldY, x => emeraldY = x, 0.3f, Ease.QuadIn)
						.Delay = 3;
				};
				
				return false;
			}
			
			if (Died || Context.Run.Won) {
				return false;
			}
			
			if (e is DiedEvent { Who: Mob }) {
				Context.Run.KillCount++;
			}

			return false;
		}
	}
}
