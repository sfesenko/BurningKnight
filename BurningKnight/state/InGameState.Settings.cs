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
		private void AddSettings() {
			var sx = Display.UiWidth * 1.5f;
			var space = 24f;
			var sy = Display.UiHeight * 0.5f - space;
			
			pauseMenu.Add(new UiLabel {
				LocaleLabel = "settings",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			pauseMenu.Add(new UiButton {
				LocaleLabel = "game",
				RelativeCenterX = sx,
				RelativeCenterY = sy - space,
				Click = b => {
					currentBack = gameBack;
					gameSettings.Enabled = true;
					SlideTo(-Display.UiWidth * 2);
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "graphics",
				RelativeCenterX = sx,
				RelativeCenterY = sy,
				Click = b => {
					currentBack = graphicsBack;
					graphicsSettings.Enabled = true;
					SlideTo(-Display.UiWidth * 2);
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "audio",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space,
				Click = b => {
					currentBack = audioBack;
					audioSettings.Enabled = true;
					SlideTo(-Display.UiWidth * 2);
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "input",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space * 2,
				Click = b => {
					currentBack = inputBack;
					inputSettings.Enabled = true;
					SlideTo(-Display.UiWidth * 2);
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "language",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space * 3,
				Click = b => {
					currentBack = languageBack;
					languageSettings.Enabled = true;
					SlideTo(-Display.UiWidth * 2);
				}
			});

			settingsBack = (UiButton) pauseMenu.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					// Synchronous: the global save is a dictionary, and a worker would race it.
					SaveManager.Save(Area, SaveType.Global);
					
					currentBack = pauseBack;
					pauseMenu.Enabled = true;
					
					SlideTo(0);
				}
			});
			
			pauseMenu.Enabled = false;
			
			AddGameSettings();
			AddGraphicsSettings();
			AddAudioSettings();
			AddInputSettings();
			AddLanguageSettings();
		}
		// The settings panes drill one UiWidth left per level; sliding back reveals the right one.
		private void SlideTo(float targetX, Action? end = null) {
			Tween.To(targetX, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
				end?.Invoke();
				SelectFirst();
			};
		}

		// Every settings checkbox is a name, a getter/setter over Settings and a position;
		// `after` covers rows with side effects (engine toggles, rumble stop), `tick`
		// replaces the update sync when a row mirrors external state (see fullscreen).
		private UiCheckbox CheckRow(UiPane pane, string name, float x, float y, Func<bool> get, Action<bool> set, Action<UiCheckbox>? after = null, Action<UiCheckbox>? tick = null) {
			var row = new UiCheckbox {
				Name = name,
				On = get(),
				RelativeX = x,
				RelativeCenterY = y,
				Click = b => {
					var c = (UiCheckbox) b;
					set(c.On);
					after?.Invoke(c);
				},
				OnUpdate = c => {
					var box = (UiCheckbox) c;

					if (tick != null) {
						tick(box);
					} else {
						box.On = get();
					}
				}
			};

			pane.Add(row);
			return row;
		}

		private void DismissConfirm() {
			if (confirmationPane == null) {
				return;
			}

			confirmationPane.Active = false;
			pauseMenu.Remove(confirmationPane);
			confirmationPane = null;
			SelectFirst();
		}

		private void AddGraphicsSettings() {
			pauseMenu.Add(graphicsSettings = new UiPane {
				RelativeX = Display.UiWidth * 2	
			});
			
			var sx = Display.UiWidth * 0.5f;
			var space = 15f;
			var sy = Display.UiHeight * 0.5f - space * 4.5f;
			
			graphicsSettings.Add(new UiLabel {
				LocaleLabel = "graphics",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			// Windowed mode does not exist on a handheld; the platform's core says whether the
			// setting applies.
			if (Engine.Instance.CanToggleFullscreen) {
				CheckRow(graphicsSettings, "fullscreen", sx, sy - space,
					() => Engine.Graphics.IsFullScreen, v => Settings.Fullscreen = v,
					c => {
						if (c.On) {
							Engine.Instance.SetFullscreen();
						} else {
							Engine.Instance.SetWindowed(Display.Width * 3, Display.Height * 3);
						}
					},
					c => {
						c.On = Engine.Graphics.IsFullScreen;
						Settings.Fullscreen = c.On;
					});
			}

			/*graphicsSettings.Add(new UiCheckbox {
				Name = "vsync",
				On = Settings.Vsync,
				RelativeX = sx,
				RelativeCenterY = sy - space,
				Click = b => {
					Settings.Vsync = ((UiCheckbox) b).On;
					Engine.Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
					Engine.Graphics.ApplyChanges();
				}
			});*/

			CheckRow(graphicsSettings, "fps", sx, sy,
				() => Settings.ShowFps, v => Settings.ShowFps = v);
			
			graphicsSettings.Add(new UiChoice {
				Name = "cursor",
				Options = new [] {
					"A", "B", "C", "D", "E", "F", "G", "J", "K"
				},
				
				Option = Settings.Cursor,
				RelativeX = sx,
				RelativeCenterY = sy + space,
				
				Click = c => {
					Settings.Cursor = ((UiChoice) c).Option;
				}
			});
			
			graphicsSettings.Add(new UiChoice {
				Name = "quality",
				Options =
				[
					"normal", "potato"
				],
				
				Option = Settings.LowQuality ? 1 : 0,
				RelativeX = sx,
				RelativeCenterY = sy + space * 2,
				
				Click = c => {
					Settings.LowQuality = ((UiChoice) c).Option == 1;
				}
			});

			UiSlider.Make(graphicsSettings, sx, sy + space * 3, "screenshake", (int) (Settings.Screenshake * 100), 1000).OnValueChange = s => {
				Settings.Screenshake = s.Value / 100f;
				ShakeComponent.Modifier = Settings.Screenshake;

				if (s.Value == 1000) {
					Achievements.Unlock("bk:overshake");
				}
			};
				
			UiSlider.Make(graphicsSettings, sx, sy + space * 4, "scale", (int) (Settings.GameScale * 100), 200, 100).OnValueChange = s => {
				Tween.To(s.Value / 100f, Settings.GameScale, x => Settings.GameScale = x, 0.3f);
			};
			
			UiSlider.Make(graphicsSettings, sx, sy + space * 5, "floor_brightness", (int) (Settings.FloorDarkness * 100), 100).OnValueChange = s => {
				Tween.To(s.Value / 100f, Settings.FloorDarkness, x => Settings.FloorDarkness = x, 0.3f);
			};

			CheckRow(graphicsSettings, "pixel_perfect", sx, sy + space * 6,
				() => Settings.PixelPerfect, v => Settings.PixelPerfect = v,
				c => Engine.Instance.UpdateView());

			CheckRow(graphicsSettings, "vsync", sx, sy + space * 7,
				() => Settings.Vsync, v => Settings.Vsync = v,
				c => {
					Engine.Graphics.SynchronizeWithVerticalRetrace = c.On;
					Engine.Graphics.ApplyChanges();
				});

			CheckRow(graphicsSettings, "flashes", sx, sy + space * 8,
				() => Settings.Flashes, v => Settings.Flashes = v,
				c => Engine.Flashes = c.On);

			CheckRow(graphicsSettings, "Vignette", sx, sy + space * 9,
				() => Settings.Vignette, v => Settings.Vignette = v,
				c => Shaders.Screen.Parameters["vignette"].SetValue(c.On));

			
			graphicsBack = (UiButton) graphicsSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = settingsBack;
					SlideTo(-Display.UiWidth, () => graphicsSettings.Enabled = false);
				}
			});
			
			graphicsSettings.Enabled = false;
		}
		private void AddAudioSettings() {
			pauseMenu.Add(audioSettings = new UiPane {
				RelativeX = Display.UiWidth * 2	
			});
			
			var sx = Display.UiWidth * 0.5f;
			var space = 20f;
			var sy = Display.UiHeight * 0.5f - space;
			
			audioSettings.Add(new UiLabel {
				LocaleLabel = "audio",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});
			
			UiSlider.Make(audioSettings, sx, sy - space, "master_volume", (int) (Settings.MasterVolume * 100)).OnValueChange = s => {
				Settings.MasterVolume = s.Value / 100f;
				UpdateRainVolume();
			};
			
			UiSlider.Make(audioSettings, sx, sy, "music", (int) (Settings.MusicVolume * 100)).OnValueChange = s => {
				Settings.MusicVolume = s.Value / 100f;
				UpdateRainVolume();
			};
			
			UiSlider.Make(audioSettings, sx, sy + space, "sfx", (int) (Settings.SfxVolume * 100)).OnValueChange = s => {
				Settings.SfxVolume = s.Value / 100f;
			};

			CheckRow(audioSettings, "ui_sfx", sx, sy + space * 2.5f,
				() => Settings.UiSfx, v => Settings.UiSfx = v);
			
			audioBack = (UiButton) audioSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = settingsBack;
					SlideTo(-Display.UiWidth, () => audioSettings.Enabled = false);
				}
			});
			
			audioSettings.Enabled = false;
		}
		private void AddLanguageSettings() {
			pauseMenu.Add(languageSettings = new UiPane {
				RelativeX = Display.UiWidth * 2
			});

			languageSettings.Add(new UiLabel {
				LocaleLabel = "language",
				RelativeCenterX = Display.UiWidth * 0.5f,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			var l = new List<string>(Languages);
			
			if (Achievements.IsComplete("bk:quackers")) {
				l.Add("qu");
			}

			for (var i = 0; i < l.Count; i++) {
				var lng = l[i];
				
				languageSettings.Add(new UiImageButton {
					Id = lng,
					RelativeCenterX = Display.UiWidth * 0.5f + 30 * (i % 2 == 0 ? -1 : 1),
					RelativeCenterY = (Display.UiHeight - Languages.Length * 20) * 0.5f + (int) Math.Floor(i / 2f) * 40,
					Click = (b) => {
						Settings.Language = lng;
						Locale.Load(lng);
						Settings.Save();
						// languageBack.Click(languageBack);

						var d = Context.Run.Depth;
						Context.Run.RealDepth = -1;
						Context.Run.Depth = d;
					}
				});
			}
			
			languageBack = (UiButton) languageSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = Display.UiWidth * 0.5f,
				RelativeCenterY = BackY,
				Click = b => {
					pauseMenu.Enabled = true;
					currentBack = settingsBack;
					
					SlideTo(-Display.UiWidth, () => languageSettings.Enabled = false);
				}
			});

			languageSettings.Enabled = false;
		}
	}
}
