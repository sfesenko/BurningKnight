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
		private void AddInputSettings() {
			pauseMenu.Add(inputSettings = new UiPane {
				RelativeX = Display.UiWidth * 2	
			});
			
			var sx = Display.UiWidth * 0.5f;
			const float space = 20f;
			var sy = Display.UiHeight * 0.5f - space * 0.5f;
			
			inputSettings.Add(new UiLabel {
				LocaleLabel = "input",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			var first = true;
			UiButton? gamepad = null;
			
			inputSettings.Add(new UiChoice {
				Name = "gamepad",
				
				RelativeX = sx,
				RelativeCenterY = sy - space,
				
				Options = ["none"],
				
				Click = c => {
					var p = LocalPlayer.Locate(Area);
					
					// Settings.Gamepad = e ? null : GamepadData.Identifiers[i];
					if (p != null) {
						var d = p.GetComponent<GamepadComponent>();
						d!.Controller?.StopRumble();
						d.Controller = null;
						d.GamepadId = null;
					}
				},
				
				OnUpdate = uc => {
					
					if (!first && !GamepadData.WasChanged) {
						return;
					}

					var con = new List<string>();
					var id = new List<string>();
					var cur = 0;
			
					for (var i = 0; i < 4; i++) {
						if (Input.Gamepads[i].Attached) {
							var d = GamePad.GetCapabilities(i);
					
							if (d.GamePadType == GamePadType.GamePad) {
								id.Add(d.Identifier);
								con.Add(d.DisplayName);
	
								if (Settings.Gamepad == d.Identifier) {
									cur = i;
								}
							}
						}
					}

					GamepadData.Identifiers = id.ToArray();
					con.Add("none");

					uc.Options = con.ToArray();
					uc.Option = cur;

					if (first && cur == con.Count - 1) {
						// gamepad.Visible = gamepad.Active = false;
					}
					
					first = false;
					GamepadData.Identifiers = id.ToArray();
				}
			});
			
			sy += space * 0.5f;
			
			inputSettings.Add(new UiButton {
				LocaleLabel = "keyboard_controls",
				RelativeCenterX = sx,
				RelativeCenterY = sy,
				Click = b => {
					currentBack = keyboardBack;
					keyboardSettings.Enabled = true;
					Tween.To(-Display.UiWidth * 3, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						inputSettings.Enabled = false;
					};
				}
			});
			
			gamepad = (UiButton) inputSettings.Add(new UiButton {
				LocaleLabel = "gamepad_controls",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space,
				Click = b => {
					currentBack = gamepadBack;
					gamepadSettings.Enabled = true;
					Tween.To(-Display.UiWidth * 3, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						inputSettings.Enabled = false;
					};
				}
			});
			
			// gamepad.Visible = GamepadComponent.Current != null || Settings.Gamepad != null;
			
			inputBack = (UiButton) inputSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = settingsBack;
					Tween.To(-Display.UiWidth, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						inputSettings.Enabled = false;
					};
				}
			});

			inputSettings.Enabled = false;

			AddKeyboardSettings();
			AddGamepadSettings();
		}
		private void AddKeyboardSettings() {
			pauseMenu.Add(keyboardSettings = new UiPane {
				RelativeX = Display.UiWidth * 3
			});
			
			var sx = Display.UiWidth * 0.5f;
			var space = 20f;
			var spX = 96f;
			var sy = Display.UiHeight * 0.5f + space * 1.5f;
			
			keyboardSettings.Add(new UiLabel {
				LocaleLabel = "keyboard",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});
			
			keyboardSettings.Add(new UiControl {
					Key = Controls.Left,
					RelativeX = sx - spX,
					RelativeCenterY = sy - space * 4,
			});
			
			keyboardSettings.Add(new UiControl {
					Key = Controls.Right,
					RelativeX = sx + spX,
					RelativeCenterY = sy - space * 4,
			});

			keyboardSettings.Add(new UiControl {
					Key = Controls.Up,
					RelativeX = sx - spX,
					RelativeCenterY = sy - space * 3,
			});
			
			keyboardSettings.Add(new UiControl {
					Key = Controls.Down,
					RelativeX = sx + spX,
					RelativeCenterY = sy - space * 3,
			});

			keyboardSettings.Add(new UiControl {
				Key = Controls.Use,
				RelativeX = sx - spX,
				RelativeCenterY = sy - space * 2,
			});
			
			keyboardSettings.Add(new UiControl {
				Key = Controls.Active,
				RelativeX = sx + spX,
				RelativeCenterY = sy - space * 2,
			});

			keyboardSettings.Add(new UiControl {
				Key = Controls.Bomb,
				RelativeX = sx - spX,
				RelativeCenterY = sy - space,
			});
			
			keyboardSettings.Add(new UiControl {
				Key = Controls.Interact,
				RelativeX = sx + spX,
				RelativeCenterY = sy - space,
			});
			
			keyboardSettings.Add(new UiControl {
				Key = Controls.Swap,
				RelativeX = sx - spX,
				RelativeCenterY = sy,
			});
			
			keyboardSettings.Add(new UiControl {
				Key = Controls.Roll,
				RelativeX = sx + spX,
				RelativeCenterY = sy,
			});
			
			keyboardSettings.Add(new UiControl {
				Key = Controls.Duck,
				RelativeX = sx,
				RelativeCenterY = sy + space,
			});
			
			keyboardBack = (UiButton) keyboardSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					inputSettings.Enabled = true;
					currentBack = inputBack;
					Tween.To(Display.UiWidth * -2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						keyboardSettings.Enabled = false;
					};
				}
			});

			keyboardSettings.Enabled = false;
		}
		private void AddGamepadSettings() {
			pauseMenu.Add(gamepadSettings = new UiPane {
				RelativeX = Display.UiWidth * 3
			});
			
			var sx = Display.UiWidth * 0.5f;
			var space = 20f;
			var spX = 96f;
			var sy = Display.UiHeight * 0.5f;// + space * 0.5f;
			
			gamepadSettings.Add(new UiLabel {
				LocaleLabel = "gamepad",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			var g = LocalPlayer.Locate(Area)?.GetComponent<GamepadComponent>();

			gamepadSettings.Add(new UiControl {
				Key = Controls.Use,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx - spX,
				RelativeCenterY = sy - space * 3,
			});
			
			gamepadSettings.Add(new UiControl {
				Key = Controls.Active,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx + spX,
				RelativeCenterY = sy - space * 3,
			});

			gamepadSettings.Add(new UiControl {
				Key = Controls.Bomb,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx - spX,
				RelativeCenterY = sy - space * 2,
			});
			
			gamepadSettings.Add(new UiControl {
				Key = Controls.Interact,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx + spX,
				RelativeCenterY = sy - space * 2,
			});
			
			gamepadSettings.Add(new UiControl {
				Key = Controls.Swap,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx - spX,
				RelativeCenterY = sy - space,
			});
			
			gamepadSettings.Add(new UiControl {
				Key = Controls.Roll,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx + spX,
				RelativeCenterY = sy - space,
			});
			
			gamepadSettings.Add(new UiControl {
				Key = Controls.Duck,
				Gamepad = true,
				GamepadComponent = g!,
				RelativeX = sx,
				RelativeCenterY = sy,
			});
			
			gamepadSettings.Add(new UiCheckbox {
				Name = "vibration",
				On = Settings.Vibrate,
				RelativeX = sx,
				RelativeCenterY = sy + space * 1.5f,
				Click = b => {
					Settings.Vibrate = ((UiCheckbox) b).On;

					if (!Settings.Vibrate) {
						GamepadComponent.Current?.StopRumble();
					}
				},
				
				OnUpdate = c => {
					((UiCheckbox) c).On = Settings.Vibrate;
				}
			});
			
			UiSlider.Make(gamepadSettings, sx, sy + space * 2.5f, "sensivity", (int) (Settings.Sensivity * 100), 200, 10).OnValueChange = s => {
				Settings.Sensivity = s.Value / 100f;
			};
			
			UiSlider.Make(gamepadSettings, sx, sy + space * 3.5f, "cursor_radius", (int) (Settings.CursorRadius * 100), 300, 10).OnValueChange = s => {
				Settings.CursorRadius = s.Value / 100f;
			};

			gamepadBack = (UiButton) gamepadSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = inputBack;
					inputSettings.Enabled = true;
					Tween.To(Display.UiWidth * -2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						gamepadSettings.Enabled = false;
					};
				}
			});
			
			gamepadSettings.Enabled = false;
		}
	}
}
