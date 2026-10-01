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
	public partial class InGameState {
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
			
			if (!Paused && (Settings.Autosave && Context.Run.Depth > 0)) {
				saveTimer += dt;

				if (saveTimer >= AutoSaveInterval) {
					saveTimer = 0;

					indicator.HandleEvent(new SaveStartedEvent());

					// Synchronous: the savers serialise the live area, so a worker would be
					// reading lists the frame is still sorting. The write is a few dozen
					// milliseconds at most, every AutoSaveInterval seconds.
					SaveManager.Backup();

					SaveManager.Save(Area, SaveType.Global);
					SaveManager.Save(Area, SaveType.Game);
					SaveManager.Save(Area, SaveType.Level);
					SaveManager.Save(Area, SaveType.Player);

					indicator.HandleEvent(new SaveEndedEvent());
				}
			}

			if (credits is { Enabled: true }) {
				if (lastCreditsLabel!.Y <= Display.UiHeight * 0.75f) {
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
			
			if ((Paused || Died || Context.Run.Won) && UiControl.Focused == null) {
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
						UiButton? sm = null;
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

								if (Settings.UiSfx) {
									Audio.PlaySfx("ui_moving", 0.5f);
								}
							}
						}
					} else if (Input.WasPressed(Controls.UiUp, gamepad, true) || (inControl && Input.WasPressed(Controls.UiLeft, gamepad, true))) {
						UiButton? sm = null;
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
							UiButton? btn = null;
							
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

				if (Context.Run.Depth == 0) {
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
							Context.Camera!.Follow(e, CursorPriority);
						}
					};

					Context.Camera!.Detached = false;
					Tween.To(-Display.UiHeight, offset, x => offset = x, 0.5f, Ease.QuadIn).OnEnd = () => {
						Menu = false;

						Timer.Add(() => {
							foreach (var n in Area.Tagged[Tags.Npc]) {
								if (n is OldMan m) {
									m.GetComponent<DialogComponent>()!.StartAndClose("shopkeeper_6", 3);
									break;
								}
							}
						}, 0.5f);
					};
				}
			}
			
			if (!Paused) {
				if (!Died && !Context.Run.Won && Context.Run.Depth > 0) {
					Context.Run.Time = (float) (timeWas + (Engine.GameTime.TotalGameTime.TotalSeconds - startTime));
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

						if (!did && (Paused || Died || Context.Run.Won) && Input.WasPressed(Controls.UiBack, controller)) {
							if (Settings.UiSfx) {
								Audio.PlaySfx("ui_exit", 0.5f);
							}

							if (UiControl.Focused != null) {
								doCheck = true;
							} else if (currentBack != null) {
								currentBack!.Click(currentBack);
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

			Context.Run.Update();
			
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
	}
}
