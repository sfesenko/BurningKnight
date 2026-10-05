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
			
			// No keyboard on a handheld: its remap rows would be focusable dead ends.
			if (TextInput.Available) {
				inputSettings.Add(new UiButton {
					LocaleLabel = "keyboard_controls",
					RelativeCenterX = sx,
					RelativeCenterY = sy,
					Click = b => {
						currentBack = keyboardBack;
						keyboardSettings.Enabled = true;
						SlideTo(-Display.UiWidth * 3, () => inputSettings.Enabled = false);
					}
				});
			}
			
			gamepad = (UiButton) inputSettings.Add(new UiButton {
				LocaleLabel = "gamepad_controls",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space,
				Click = b => {
					currentBack = gamepadBack;
					gamepadSettings.Enabled = true;
					SlideTo(-Display.UiWidth * 3, () => inputSettings.Enabled = false);
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
					SlideTo(-Display.UiWidth, () => inputSettings.Enabled = false);
				}
			});

			inputSettings.Enabled = false;

			AddKeyboardSettings();
			AddGamepadSettings();
		}
		private static readonly (string Key, float Dx, float Dy)[] KeyControlRows = [
			(Controls.Left, -1, -4), (Controls.Right, 1, -4),
			(Controls.Up, -1, -3), (Controls.Down, 1, -3),
			(Controls.Use, -1, -2), (Controls.Active, 1, -2),
			(Controls.Bomb, -1, -1), (Controls.Interact, 1, -1),
			(Controls.Swap, -1, 0), (Controls.Roll, 1, 0),
			(Controls.Duck, 0, 1)
		];

		private static readonly (string Key, float Dx, float Dy)[] PadControlRows = [
			(Controls.Use, -1, -3), (Controls.Active, 1, -3),
			(Controls.Bomb, -1, -2), (Controls.Interact, 1, -2),
			(Controls.Swap, -1, -1), (Controls.Roll, 1, -1),
			(Controls.Duck, 0, 0)
		];

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

			foreach (var (key, dx, dy) in KeyControlRows) {
				keyboardSettings.Add(new UiControl {
					Key = key,
					RelativeX = sx + dx * spX,
					RelativeCenterY = sy + dy * space
				});
			}
			
			keyboardBack = (UiButton) keyboardSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					inputSettings.Enabled = true;
					currentBack = inputBack;
					SlideTo(Display.UiWidth * -2, () => keyboardSettings.Enabled = false);
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

			foreach (var (key, dx, dy) in PadControlRows) {
				gamepadSettings.Add(new UiControl {
					Key = key,
					Gamepad = true,
					RelativeX = sx + dx * spX,
					RelativeCenterY = sy + dy * space
				});
			}

			// No rumble without a host backend; desktop always has one (see Program.cs).
			if (Vibration.Available) {
				CheckRow(gamepadSettings, "vibration", sx, sy + space * 1.5f,
					() => Settings.Vibrate, v => Settings.Vibrate = v,
					c => {
						if (!c.On) {
							GamepadComponent.Current?.StopRumble();
						}
					});
			}
			
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
					SlideTo(Display.UiWidth * -2, () => gamepadSettings.Enabled = false);
				}
			});
			
			gamepadSettings.Enabled = false;
		}
	}
}
