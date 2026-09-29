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
					Tween.To(-Display.UiWidth * 2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = SelectFirst;
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "graphics",
				RelativeCenterX = sx,
				RelativeCenterY = sy,
				Click = b => {
					currentBack = graphicsBack;
					graphicsSettings.Enabled = true;
					Tween.To(-Display.UiWidth * 2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = SelectFirst;
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "audio",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space,
				Click = b => {
					currentBack = audioBack;
					audioSettings.Enabled = true;
					Tween.To(-Display.UiWidth * 2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = SelectFirst;
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "input",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space * 2,
				Click = b => {
					currentBack = inputBack;
					inputSettings.Enabled = true;
					Tween.To(-Display.UiWidth * 2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = SelectFirst;
				}
			});
			
			pauseMenu.Add(new UiButton {
				LocaleLabel = "language",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space * 3,
				Click = b => {
					currentBack = languageBack;
					languageSettings.Enabled = true;
					Tween.To(-Display.UiWidth * 2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = SelectFirst;
				}
			});

			settingsBack = (UiButton) pauseMenu.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					new Thread(() => {
						try {
							SaveManager.Save(Area, SaveType.Global);
						} catch (Exception e) {
							Log.Error(e);
						}
					}) {
						Priority = ThreadPriority.Lowest
					}.Start();
					
					currentBack = pauseBack;
					pauseMenu.Enabled = true;
					
					Tween.To(0, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
					};
				}
			});
			
			pauseMenu.Enabled = false;
			
			AddGameSettings();
			AddGraphicsSettings();
			AddAudioSettings();
			AddInputSettings();
			AddLanguageSettings();
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

			graphicsSettings.Add(new UiCheckbox {
				Name = "fullscreen",
				On = Engine.Graphics.IsFullScreen,
				RelativeX = sx,
				RelativeCenterY = sy - space,
				Click = b => {
					Settings.Fullscreen = ((UiCheckbox) b).On;

					if (Settings.Fullscreen) {
						Engine.Instance.SetFullscreen();
					} else {
						Engine.Instance.SetWindowed(Display.Width * 3, Display.Height * 3);
					}
				},
				
				OnUpdate = c => {
					((UiCheckbox) c).On = Engine.Graphics.IsFullScreen;
					Settings.Fullscreen = ((UiCheckbox) c).On;
				}
			});

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

			graphicsSettings.Add(new UiCheckbox {
				Name = "fps",
				On = Settings.ShowFps,
				RelativeX = sx,
				RelativeCenterY = sy,
				Click = b => {
					Settings.ShowFps = ((UiCheckbox) b).On;
				},
				
				OnUpdate = c => {
					((UiCheckbox) c).On = Settings.ShowFps;
				}
			});
			
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

			graphicsSettings.Add(new UiCheckbox {
				Name = "pixel_perfect",
				On = Settings.PixelPerfect,
				RelativeX = sx,
				RelativeCenterY = sy + space * 6,
				Click = b => {
					Settings.PixelPerfect = ((UiCheckbox) b).On;
					Engine.Instance.UpdateView();
				}
			});

			graphicsSettings.Add(new UiCheckbox {
				Name = "vsync",
				On = Settings.Vsync,
				RelativeX = sx,
				RelativeCenterY = sy + space * 7,
				Click = b => {
					Settings.Vsync = ((UiCheckbox) b).On;
					Engine.Graphics.SynchronizeWithVerticalRetrace = Settings.Vsync;
					Engine.Graphics.ApplyChanges();
				}
			});

			graphicsSettings.Add(new UiCheckbox {
				Name = "flashes",
				On = Settings.Flashes,
				RelativeX = sx,
				RelativeCenterY = sy + space * 8,
				Click = b => {
					Engine.Flashes = Settings.Flashes = ((UiCheckbox) b).On;
				}
			});
			
			graphicsSettings.Add(new UiCheckbox {
				Name = "Vignette",
				On = Settings.Vignette,
				RelativeX = sx,
				RelativeCenterY = sy + space * 9,
				Click = b => {
					Settings.Vignette = ((UiCheckbox) b).On;
					Shaders.Screen.Parameters["vignette"].SetValue(Settings.Vignette);
				}
			});

			
			graphicsBack = (UiButton) graphicsSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = settingsBack;
					Tween.To(-Display.UiWidth, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						graphicsSettings.Enabled = false;
					};
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

			audioSettings.Add(new UiCheckbox {
				Name = "ui_sfx",
				On = Settings.UiSfx,
				RelativeX = sx,
				RelativeCenterY = sy + space * 2.5f,
				Click = b => {
					Settings.UiSfx = ((UiCheckbox) b).On;
				}
			});
			
			audioBack = (UiButton) audioSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = settingsBack;
					Tween.To(-Display.UiWidth, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						audioSettings.Enabled = false;
					};
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

						var d = Run.Depth;
						Run.RealDepth = -1;
						Run.Depth = d;
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
					
					Tween.To(-Display.UiWidth, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						languageSettings.Enabled = false;
					};
				}
			});

			languageSettings.Enabled = false;
		}
	}
}
