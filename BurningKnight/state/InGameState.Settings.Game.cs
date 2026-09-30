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
		private void AddGameSettings() {
			pauseMenu.Add(gameSettings = new UiPane {
				RelativeX = Display.UiWidth * 2	
			});
			
			var sx = Display.UiWidth * 0.5f;
			var space = 18f;
			var sy = Display.UiHeight * 0.5f - space * 1.5f - 10;
			
			gameSettings.Add(new UiLabel {
				LocaleLabel = "game",
				RelativeCenterX = sx,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			gameSettings.Add(new UiCheckbox {
				Name = "autosave",
				On = Settings.Autosave,
				RelativeX = sx,
				RelativeCenterY = sy - space * 2,
				Click = b => {
					Settings.Autosave = ((UiCheckbox) b).On;
				}
			});

			gameSettings.Add(new UiCheckbox {
				Name = "autopause",
				On = Settings.Autopause,
				RelativeX = sx,
				RelativeCenterY = sy - space,
				Click = b => {
					Settings.Autopause = ((UiCheckbox) b).On;
				}
			});

			gameSettings.Add(new UiCheckbox {
				Name = "speedrun_timer",
				On = Settings.SpeedrunTimer,
				RelativeX = sx,
				RelativeCenterY = sy,
				Click = b => {
					Settings.SpeedrunTimer = ((UiCheckbox) b).On;
				}
			});

			var presses = 0;

			gameSettings.Add(new UiCheckbox {
				Name = "vegan_mode",
				On = Settings.Vegan,
				RelativeX = sx,
				RelativeCenterY = sy + space,
				Click = b => {
					presses++;
					Settings.Vegan = ((UiCheckbox) b).On;

					Log.Info($"Click #{presses}");
					
					if (presses == 20) {
						Log.Debug("Unlock npcs!");
						
						GlobalSave.Put(ShopNpc.AccessoryTrader, true);
						GlobalSave.Put(ShopNpc.ActiveTrader, true);
						GlobalSave.Put(ShopNpc.HatTrader, true);
						GlobalSave.Put(ShopNpc.WeaponTrader, true);
						GlobalSave.Put(ShopNpc.Mike, true);
						
						GlobalSave.Put("control_use", true);
						GlobalSave.Put("control_swap", true);
						GlobalSave.Put("control_roll", true);
						GlobalSave.Put("control_interact", true);
						GlobalSave.Put("control_duck", true);
					}
				}
			});

			gameSettings.Add(new UiCheckbox {
				Name = "blood_n_gore",
				On = Settings.Blood,
				RelativeX = sx,
				RelativeCenterY = sy + space * 2,
				Click = b => {
					Settings.Blood = ((UiCheckbox) b).On;
				}
			});
			
			gameSettings.Add(new UiCheckbox {
				Name = "minimap",
				On = Settings.Minimap,
				RelativeX = sx,
				RelativeCenterY = sy + space * 3,
				Click = b => {
					Settings.Minimap = ((UiCheckbox) b).On;
				}
			});
			
			gameSettings.Add(new UiButton {
				LocaleLabel = "reset_settings",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space * 4.5f,
				Click = b => {
					GoConfirm("reset_settings_dis", () => {
						currentBack = settingsBack;
						gameSettings.Enabled = true;
						
						// Synchronous: this writes the controls file and regenerates the
						// settings — all main-thread state, a worker would race the frame.
						var d = Context.Run.Depth;
						Context.Run.RealDepth = -1;
						Context.Run.Depth = d;
						Controls.BindDefault();
						Controls.Save();
						Settings.Generate();
					}, () => {
						currentBack = gameBack;
						gameSettings.Enabled = true;

						Tween.To(Display.UiWidth * -2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
							pauseMenu.Remove(confirmationPane);
							confirmationPane = null;	
							SelectFirst();
						};
					});
				}
			});
			
			gameSettings.Add(new UiButton {
				LocaleLabel = "reset_progress",
				RelativeCenterX = sx,
				RelativeCenterY = sy + space * 5.5f,
				Click = b => {
					GoConfirm("reset_progress_dis", () => {
						currentBack = settingsBack;
						gameSettings.Enabled = true;
						
						Achievements.ItemBuffer.Clear();
						Achievements.AchievementBuffer.Clear();
						
						// Synchronous: deleting the saves and resetting the run state are quick,
						// and a worker would race every frame that reads Run.
						SaveManager.Delete(SaveType.Player, SaveType.Level, SaveType.Game, SaveType.Global);
						CloudSave.Delete();

						try {
							Stats.Reset();
						} catch (Exception e) {
							Log.Error(e);
						}
						
						Achievements.LoadState();
						GlobalSave.Emeralds = 0;
						
						Context.Run.StartingNew = true;
						Context.Run.NextDepth = 0;
						Context.Run.IntoMenu = true;
						Settings.Setup();
					}, () => {
						currentBack = gameBack;
						gameSettings.Enabled = true;

						Tween.To(Display.UiWidth * -2, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
							confirmationPane.Active = false;
							pauseMenu.Remove(confirmationPane);
							confirmationPane = null;	
							SelectFirst();
						};
					});
				}
			});
		
			gameSettings.Add(new UiButton {
					LocaleLabel = "credits",
					RelativeCenterX = sx,
					RelativeCenterY = sy + space * 6.5f,
					Click = b => {
						SetupCredits();
						credits.Enabled = true;
						
						Tween.To(Display.UiWidth * -3, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
							gameSettings.Enabled = false;
						};
					}
			});
			
			if (Context.Run.Depth == 0) {
				gameSettings.Add(new UiButton {
						LocaleLabel = "tutorial",
						RelativeCenterX = sx,
						RelativeCenterY = sy + space * 7.5f,
						Click = b => { Context.Run.Depth = -2; }
				});
			}

			gameBack = (UiButton) gameSettings.Add(new UiButton {
				LocaleLabel = "back",
				Type = ButtonType.Exit,
				RelativeCenterX = sx,
				RelativeCenterY = BackY,
				Click = b => {
					currentBack = settingsBack;
					Tween.To(-Display.UiWidth, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
						SelectFirst();
						gameSettings.Enabled = false;
					};
				}
			});
			
			gameSettings.Enabled = false;
		}
		private void GoConfirm(string text, Action callback, Action nope) {
			pauseMenu.Add(confirmationPane = new UiPane {
				RelativeX = Display.UiWidth * 3
			});
			
			var sx = Display.UiWidth * 0.5f;
			var sy = Display.UiHeight * 0.5f;
			var space = 32;
			
			confirmationPane.Add(new UiLabel {
				Font = Font.Small,
				AngleMod = 0,
				LocaleLabel = "are_you_sure",
				RelativeCenterX = sx,
				RelativeCenterY = sy - space * 1.5f,
				Clickable = false
			});

			confirmationPane.Add(new UiLabel {
				Font = Font.Small,
				AngleMod = 0,
				LocaleLabel = text,
				RelativeCenterX = sx,
				RelativeCenterY = sy - space,
				Clickable = false
			});

			var spx = 32;
			
			confirmationPane.Add(new UiButton {
				LocaleLabel = "yes",
				RelativeCenterX = sx + spx,
				RelativeCenterY = sy + space,
				Click = b => {
					callback();
				}
			});
			
			currentBack = (UiButton) confirmationPane.Add(new UiButton {
				LocaleLabel = "no",
				RelativeCenterX = sx - spx,
				RelativeCenterY = sy + space,
				Click = b => {
					nope();
				}
			});
			
			Tween.To(Display.UiWidth * -3, pauseMenu.X, x => pauseMenu.X = x, PaneTransitionTime).OnEnd = () => {
				SelectFirst();
				gameSettings.Enabled = false;
			};
		}
	}
}
