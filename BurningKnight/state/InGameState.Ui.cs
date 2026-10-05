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
		private void SetupUi() {
			// Chat opens on typed '/' and there is no text source on a handheld.
			if (TextInput.Available) {
				TopUi.Add(new UiChat());
			}
			
			UiButton.LastId = 0;
			
			Camera = new Camera(new FollowingDriver());
			TopUi.Add(Camera);
			// Ui.Add(new AchievementBanner());

			CreateEditor(Camera);

			var id = Context.Level!.Biome!.Id;

			if (id != Biome.Castle && id != Biome.Hub) {
				Achievements.Unlock($"bk:{id}");
				var i = Context.Level!.Biome.GetItemUnlock();

				if (i != null) {
					Items.Unlock(i);
				}
			}

			foreach (var p in Area.Tagged[Tags.Player]) {
				var cursor = new Cursor {
					Player = (Player) p
				};
				
				TopUi.Add(cursor);
				p.GetComponent<CursorComponent>()!.Cursor = cursor;
			}
			
			Ui.Add(indicator = new SaveIndicator());

			var player = LocalPlayer.Locate(Area);

			if (!Multiplayer && Context.Run.Depth > 0) {
				Ui.Add(map = new UiMap(player!));
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
			var start = Display.UiHeight * 0.5f + (Context.Run.Depth > 0 ? -space * 0.5f : space);

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

			if (Context.Run.Depth > 0) {
				scoreLabel = (UiLabel) pauseMenu.Add(new UiLabel {
					Font = Font.Small,
					Label = GetScore(),
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = TitleY + 24,
					Clickable = false,
					AngleMod = 0
				});
			}

			if (Context.Run.Depth > 0) {
				pauseMenu.Add(seedLabel = new UiButton {
					Font = Font.Small,
					Selectable = false,
					Label = $"Seed: {Context.Run.Seed}",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = BackY,
					AngleMod = 0,
					Click = b => {
						// No clipboard backend on this host: leave the seed label alone.
						if (!Clipboard.Available) {
							return;
						}

						b.LocaleLabel = "copied_to_clipboard";

						try {
							// Needs xclip on linux
							Clipboard.SetText(Context.Run.Seed!);
						} catch (Exception e) {
							Log.Error(e);
						}

						Timer.Add(() => { b.Label = $"{Locale.Get("seed")}: {Context.Run.Seed}"; }, 0.5f);
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
					SlideTo(-Display.UiWidth);
				}
			});
			
			if (Context.Run.Depth > 0) {
			
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
			
				if (Context.Run.Type != RunType.Daily) {
					pauseMenu.Add(new UiButton {
						LocaleLabel = "new_run",
						RelativeCenterX = Display.UiWidth / 2f,
						RelativeCenterY = start + space * 2,
						Type = ButtonType.Exit,
						Click = b => GoConfirm("start_new_run", () => { Context.Run.StartNew(); }, () => {
							currentBack = pauseBack;
							pauseMenu.Enabled = true;
							SlideTo(0, DismissConfirm);
						})
					});
				}
			} else if (Context.Run.Depth == 0) {
				pauseMenu.Add(new UiButton {
					LocaleLabel = "exit",
					Type = ButtonType.Exit,
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = BackY,
					Click = b => {
						Engine.Instance.Quit();
					}
				});
			}

			if (Context.Run.Depth != 0) {
				pauseMenu.Add(new UiButton {
					LocaleLabel = "back_to_town",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = start + space * 3,
					Type = ButtonType.Exit,
					Click = b => Context.Run.Depth = 0
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

			var qr = Context.Run.Depth > 0 && (Context.Run.Type == RunType.Regular || Context.Run.Type == RunType.Challenge || Context.Run.Type == RunType.BossRush);
			
			if (qr) {
				gameOverMenu.Add(overQuickBack = new UiButton {
					Font = Font.Small,
					LocaleLabel = "quick_restart",
					RelativeCenterX = Display.UiWidth / 2f,
					RelativeCenterY = BackY - 6,

					Click = b => {
						if (Context.Run.Type == RunType.BossRush) {
							if (GlobalSave.Emeralds < 3) {
								AnimationUtil.ActionFailed();
								return;
							}
							
							GlobalSave.Emeralds -= 3;
						}

						gameOverMenu.Enabled = false;
						Context.Run.StartNew(1, Context.Run.Type);
					}
				});
			}

			gameOverMenu.Add(overBack = new UiButton {
				LocaleLabel = "back_to_town",
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = BackY + (qr ? 12 : 0),

				Click = b => {
					gameOverMenu.Enabled = false;
					Context.Run.StartNew(Context.Run.Depth == -2 ? -2 : 0);
				}
			});

			gameOverMenu.Setup();
			gameOverMenu.Enabled = false;

			if (Context.Run.Depth > 0 && Context.Level != null && !Menu) {
				Ui.Add(new UiBanner(Level.GetDepthString()));
			}
			
			leaderMenu.Add(boardType = new UiLabel {
				Label = $"{Locale.Get($"run_{Context.Run.Type.ToString().ToLower()}")} {Locale.Get("leaderboard")}",
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
				string? lastS = null;
				
				d = (s) =>{
					if (s == null) {
						s = lastS ?? Context.Run.GetLeaderboardId();
					}

					lastS = s;
					
					loading.Hide = false;
					choice.Disabled = true;
					
					leaderStats.Clear();
					offset = Math.Max(0, offset);

					SetupLeaderboard(leaderStats, s, choice!.Options![choice.Option], offset, () => {
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
			
			if (Context.Run.Depth == 0) {
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
	}
}
