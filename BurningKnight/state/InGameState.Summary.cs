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
		public void ShowStats(int place, string id) {
			if (sbusy) {
				return;
			}

			sbusy = true;

			statsStats.Clear();

			placeLabel.Label = $"{Locale.Get("top")} #{place + 1} {Locale.Get("run")}";
			placeLabel.RelativeCenterX = Display.UiWidth * 0.5f;

			try {
				var score = GlobalSave.GetInt(id);
				var data = GlobalSave.GetJson($"{id}_data");

				statsStats.Add(Locale.Get("seed"), data!["seed"].String(), false, bt => {
					// No clipboard backend on this host: leave the seed row alone.
					if (!Clipboard.Available) {
						return;
					}

					var b = (UiTableEntry) bt;
					b.RealLocaleLabel = "copied_to_clipboard";

					try {
						// Needs xclip on linux
						Clipboard.SetText(Context.Run.Seed!);
					} catch (Exception e) {
						Log.Error(e);
					}

					Timer.Add(() => b.RealLocaleLabel = "seed", 0.5f);
				});

				statsStats.Add(Locale.Get("won"), Locale.Get(data["won"].AsBoolean() ? "yes" : "no"));
				statsStats.Add(Locale.Get("time"), data["time"].String());
				statsStats.Add(Locale.Get("lamp"), Locale.Get(data["lamp"].String("none")));
				statsStats.Add(Locale.Get("depth"), data["depth"].String("old data"));
				statsStats.Add(Locale.Get("coins_collected"), data["coins"].AsNumber().ToString());
				statsStats.Add(Locale.Get("items_collected"), data["items"].AsNumber().ToString());
				statsStats.Add(Locale.Get("damage_taken"), data["damage"].AsNumber().ToString());
				statsStats.Add(Locale.Get("kills"), data["kills"].AsNumber().ToString());
				statsStats.Add(Locale.Get("scourge_stats"), data["scourge"].AsNumber().ToString());
				statsStats.Add(Locale.Get("rooms_explored"), data["rooms"].String());
				statsStats.Add(Locale.Get("distance_traveled"), data["distance"].String());
				statsStats.Add(Locale.Get("score"), score.ToString());
				
			} catch (Exception e) {
				statsStats.Add("Error", e.Message);
				Log.Error(e);
			}

			statsStats.Prepare();

			statsStats.RelativeCenterX = Display.UiWidth * 0.5f;
			statsStats.RelativeCenterY = Display.UiHeight * 0.5f;

			statsMenu.Enabled = true;
			currentBack = statsBack;
			statsMenu.Y = Display.UiHeight;
			animating = true;
			
			Tween.To(0, statsMenu.Y, x => statsMenu.Y = x, 1f, Ease.BackOut).OnEnd = () => {
				SelectFirst();
				animating = false;
			};
		}
		public void AnimateDoneScreen(Player player) {
			if (Context.Run.Type == RunType.Daily) {
				for (var i = 0; i < Player.MaxPlayers; i++) {
					Player.StartingItems[i] = null;
					Player.StartingWeapons[i] = null;
					Player.StartingLamps[i] = null;
				}

				Player.DailyItems = null;
			}

			if (map != null) {
				map.Done = true;
			}

			Tween.To(0, emeraldY, x => emeraldY = x, 0.4f, Ease.BackOut);
			
			GlobalSave.Put("run_count", GlobalSave.GetInt("run_count") + 1);

			var lamp = "none";

			try {
				var l = player.GetComponent<LampComponent>()!.Item;

				if (l != null) {
					lamp = l.Id;
				}
			} catch (Exception e) {
				Log.Error(e);
			}

			if (Context.Run.Won) {
				if (Context.Run.Type == RunType.BossRush) {
					Achievements.Unlock("bk:boss_rush");
				} else if (Context.Run.Type == RunType.Challenge) {
					GlobalSave.Put($"challenge_{Context.Run.ChallengeId}", true);
					var count = 0;

					for (var i = 1; i <= 30; i++) {
						if (GlobalSave.IsTrue($"challenge_{i}")) {
							count++;
						}
					}
					
					Achievements.SetProgress("bk:10_challenges", Math.Min(10, count), 10);
					Achievements.SetProgress("bk:20_challenges", Math.Min(20, count), 20);
					Achievements.SetProgress("bk:30_challenges", Math.Min(30, count), 30);
				} else if (Context.Run.Type == RunType.Daily) {
					Achievements.Unlock("bk:daily");
				} else if (Context.Run.Type == RunType.Regular) {
					if (player.GetComponent<LampComponent>()!.Item?.Id != "bk:no_lamp") {
						Achievements.Unlock("bk:unstoppable");
					}

					if (!(GameSave.IsTrue("sk_enraged") || GameSave.IsTrue("item_stolen"))) {
						Achievements.Unlock("bk:not_a_thief");
					}
				}
			}

			gameOverMenu.Enabled = true;	
			GlobalSave.Put("played_once", true);

			gameOverMenu.Add(new UiLabel {
				LocaleLabel = Context.Run.Won ? (BK.Demo ? "you_won_demo" : "won_message") : "death_message",
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			Context.Camera!.Targets.Clear();

			var stats = new UiTable();

			gameOverMenu.Add(stats);

			stats.Add(Locale.Get("run_type"), Locale.Get($"run_{Context.Run.Type.ToString().ToLower()}") + (Context.Run.CustomSeed ? " " + Locale.Get("seeded") : ""));
			stats.Add(Locale.Get("seed"), Context.Run.Seed!, false, bt => {
				// No clipboard backend on this host: leave the seed row alone.
				if (!Clipboard.Available) {
					return;
				}

				var b = (UiTableEntry) bt;
				b.RealLocaleLabel = "copied_to_clipboard";

				try {
					// Needs xclip on linux
					Clipboard.SetText(Context.Run.Seed!);
				} catch (Exception e) {
					Log.Error(e);
				}

				Timer.Add(() => b.RealLocaleLabel = "seed", 0.5f);
			});
			
			stats.Add(Locale.Get("lamp"), Locale.Get(lamp));
			stats.Add(Locale.Get("time"), GetRunTime());
			stats.Add(Locale.Get("depth"), Level.GetDepthString(true));
			stats.Add(Locale.Get("coins_collected"), Context.Run.Statistics!.CoinsObtained!.ToString());
			stats.Add(Locale.Get("items_collected"), Context.Run.Statistics.Items.Count.ToString());
			stats.Add(Locale.Get("damage_taken"), Context.Run.Statistics.DamageTaken.ToString());
			stats.Add(Locale.Get("kills"), Context.Run.Statistics.MobsKilled.ToString());
			stats.Add(Locale.Get("scourge_stats"), Context.Run.Scourge.ToString());
			stats.Add(Locale.Get("rooms_explored"), $"{Context.Run.Statistics.RoomsExplored} / {Context.Run.Statistics.RoomsTotal}");
			stats.Add(Locale.Get("distance_traveled"), $"{(Context.Run.Statistics.TilesWalked / 1024f):0.0} {Locale.Get("km")}");

			Context.Run.CalculateScore();
			Log.Info($"Run score is {Context.Run.Score}");

			currentBack = overBack;
			var newHigh = false;

			if (Context.Run.Type == RunType.Regular) {
				newHigh = GlobalSave.GetInt("high_score") < Context.Run.Score;
				
				if (newHigh) {
					Log.Info("New highscore!");
					GlobalSave.Put("high_score", Context.Run.Score);
				}
			}
			
			var board = Context.Run.GetLeaderboardId();
			
			stats.Add(Locale.Get("score"), newHigh ? $"{Locale.Get("new_high_score")} {Context.Run.Score}" : Context.Run.Score.ToString(), newHigh, b => {
				ShowLeaderboard(board, board);

				Tween.To(-Display.UiHeight, gameOverMenu.Y, x => gameOverMenu.Y = x, 0.6f).OnEnd = () => {
					gameOverMenu.Enabled = false;
				};

				ReturnFromLeaderboard = () => {
					gameOverMenu.Enabled = true;
					currentBack = overBack;
					Tween.To(0, gameOverMenu.Y, x => gameOverMenu.Y = x, 1f, Ease.BackOut);
				};
			});
			
			stats.Prepare();
			
			stats.RelativeCenterX = Display.UiWidth * 0.5f;
			stats.RelativeCenterY = Display.UiHeight * 0.5f;
			
			if (Context.Run.Type == RunType.Regular) {
				var place = -1;

				for (var i = 0; i < 3; i++) {
					var id = $"top_{i}";
						
					if (!GlobalSave.Exists(id) || GlobalSave.GetInt(id) < Context.Run.Score) {
						place = i;
						break;
					}
				}

				if (place != -1) {
					if (place < 2) {
						for (var i = 1; i >= place; i--) {
							var id1 = $"top_{i}";
							var id2 = $"top_{i + 1}";

							GlobalSave.Put(id2, GlobalSave.GetInt(id1));
							GlobalSave.Put($"{id2}_data", GlobalSave.GetString($"{id1}_data")!);
						}
					}

					Log.Info($"New #{place} run!");

					var root = new JsonObject();

					root["seed"] = Context.Run.Seed;
					root["time"] = GetRunTime();
					root["depth"] = Level.GetDepthString(true);
					root["won"] = Context.Run.Won;

					root["lamp"] = lamp;
					root["coins"] = Context.Run.Statistics.CoinsObtained;
					root["items"] = Context.Run.Statistics.Items.Count;
					root["damage"] = Context.Run.Statistics.DamageTaken;
					root["kills"] = Context.Run.Statistics.MobsKilled;
					root["rooms"] = $"{Context.Run.Statistics.RoomsExplored} / {Context.Run.Statistics.RoomsTotal}";
					root["scourge"] = Context.Run.Scourge;
					root["distance"] = $"{(Context.Run.Statistics.TilesWalked / 1024f):0.0} {Locale.Get("km")}";

					var id = $"top_{place}";

					GlobalSave.Put(id, Context.Run.Score);
					GlobalSave.Put($"{id}_data", root.ToString());
				}
			}
			
			if (Context.Run.Won) {
				killedLabel.Done = true;
				Killer.Done = true;

				// Synchronous: a handful of small files, and Backup is a no-op.
				// SaveManager.Save(Area, SaveType.Statistics);
				SaveManager.Delete(SaveType.Player, SaveType.Level, SaveType.Game);
				SaveManager.Backup();
			}
			
			Audio.PlayMusic("Nostalgia", true);
			
			Tween.To(this, new {blur = 1}, 0.5f);
			Tween.To(0, gameOverMenu.Y, x => gameOverMenu.Y = x, 1f, Ease.BackOut).OnEnd = SelectFirst;
			
			OpenBlackBars();

			Context.Run.SubmitScore?.Invoke(Context.Run.Score, board);
		}
	}
}
