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

				statsStats.Add(Locale.Get("seed"), data["seed"].AsString, false, bt => {
					var b = (UiTableEntry) bt;
					b.RealLocaleLabel = "copied_to_clipboard";

					try {
						// Needs xclip on linux
						Clipboard.SetText(Run.Seed);
					} catch (Exception e) {
						Log.Error(e);
					}

					Timer.Add(() => b.RealLocaleLabel = "seed", 0.5f);
				});

				statsStats.Add(Locale.Get("won"), Locale.Get(data["won"].AsBoolean ? "yes" : "no"));
				statsStats.Add(Locale.Get("time"), data["time"].AsString);
				statsStats.Add(Locale.Get("lamp"), Locale.Get(data["lamp"].String("none")));
				statsStats.Add(Locale.Get("depth"), data["depth"].String("old data"));
				statsStats.Add(Locale.Get("coins_collected"), data["coins"].AsNumber.ToString());
				statsStats.Add(Locale.Get("items_collected"), data["items"].AsNumber.ToString());
				statsStats.Add(Locale.Get("damage_taken"), data["damage"].AsNumber.ToString());
				statsStats.Add(Locale.Get("kills"), data["kills"].AsNumber.ToString());
				statsStats.Add(Locale.Get("scourge_stats"), data["scourge"].AsNumber.ToString());
				statsStats.Add(Locale.Get("rooms_explored"), data["rooms"].AsString);
				statsStats.Add(Locale.Get("distance_traveled"), data["distance"].AsString);
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
			if (Run.Type == RunType.Daily) {
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
				var l = player.GetComponent<LampComponent>().Item;

				if (l != null) {
					lamp = l.Id;
				}
			} catch (Exception e) {
				Log.Error(e);
			}

			if (Run.Won) {
				if (Run.Type == RunType.BossRush) {
					Achievements.Unlock("bk:boss_rush");
				} else if (Run.Type == RunType.Challenge) {
					GlobalSave.Put($"challenge_{Run.ChallengeId}", true);
					var count = 0;

					for (var i = 1; i <= 30; i++) {
						if (GlobalSave.IsTrue($"challenge_{i}")) {
							count++;
						}
					}
					
					Achievements.SetProgress("bk:10_challenges", Math.Min(10, count), 10);
					Achievements.SetProgress("bk:20_challenges", Math.Min(20, count), 20);
					Achievements.SetProgress("bk:30_challenges", Math.Min(30, count), 30);
				} else if (Run.Type == RunType.Daily) {
					Achievements.Unlock("bk:daily");
				} else if (Run.Type == RunType.Regular) {
					if (player.GetComponent<LampComponent>().Item?.Id != "bk:no_lamp") {
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
				LocaleLabel = Run.Won ? (BK.Demo ? "you_won_demo" : "won_message") : "death_message",
				RelativeCenterX = Display.UiWidth / 2f,
				RelativeCenterY = TitleY,
				Clickable = false
			});

			Camera.Instance.Targets.Clear();

			var stats = new UiTable();

			gameOverMenu.Add(stats);

			stats.Add(Locale.Get("run_type"), Locale.Get($"run_{Run.Type.ToString().ToLower()}") + (Run.CustomSeed ? " " + Locale.Get("seeded") : ""));
			stats.Add(Locale.Get("seed"), Run.Seed, false, bt => {
				var b = (UiTableEntry) bt;
				b.RealLocaleLabel = "copied_to_clipboard";

				try {
					// Needs xclip on linux
					Clipboard.SetText(Run.Seed);
				} catch (Exception e) {
					Log.Error(e);
				}

				Timer.Add(() => b.RealLocaleLabel = "seed", 0.5f);
			});
			
			stats.Add(Locale.Get("lamp"), Locale.Get(lamp));
			stats.Add(Locale.Get("time"), GetRunTime());
			stats.Add(Locale.Get("depth"), Level.GetDepthString(true));
			stats.Add(Locale.Get("coins_collected"), Run.Statistics.CoinsObtained.ToString());
			stats.Add(Locale.Get("items_collected"), Run.Statistics.Items.Count.ToString());
			stats.Add(Locale.Get("damage_taken"), Run.Statistics.DamageTaken.ToString());
			stats.Add(Locale.Get("kills"), Run.Statistics.MobsKilled.ToString());
			stats.Add(Locale.Get("scourge_stats"), Run.Scourge.ToString());
			stats.Add(Locale.Get("rooms_explored"), $"{Run.Statistics.RoomsExplored} / {Run.Statistics.RoomsTotal}");
			stats.Add(Locale.Get("distance_traveled"), $"{(Run.Statistics.TilesWalked / 1024f):0.0} {Locale.Get("km")}");

			Run.CalculateScore();
			Log.Info($"Run score is {Run.Score}");

			currentBack = overBack;
			var newHigh = false;

			if (Run.Type == RunType.Regular) {
				newHigh = GlobalSave.GetInt("high_score") < Run.Score;
				
				if (newHigh) {
					Log.Info("New highscore!");
					GlobalSave.Put("high_score", Run.Score);
				}
			}
			
			var board = Run.GetLeaderboardId();
			
			stats.Add(Locale.Get("score"), newHigh ? $"{Locale.Get("new_high_score")} {Run.Score}" : Run.Score.ToString(), newHigh, b => {
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
			
			if (Run.Type == RunType.Regular) {
				var place = -1;

				for (var i = 0; i < 3; i++) {
					var id = $"top_{i}";
						
					if (!GlobalSave.Exists(id) || GlobalSave.GetInt(id) < Run.Score) {
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
							GlobalSave.Put($"{id2}_data", GlobalSave.GetString($"{id1}_data"));
						}
					}

					Log.Info($"New #{place} run!");

					var root = new JsonObject();

					root["seed"] = Run.Seed;
					root["time"] = GetRunTime();
					root["depth"] = Level.GetDepthString(true);
					root["won"] = Run.Won;

					root["lamp"] = lamp;
					root["coins"] = Run.Statistics.CoinsObtained;
					root["items"] = Run.Statistics.Items.Count;
					root["damage"] = Run.Statistics.DamageTaken;
					root["kills"] = Run.Statistics.MobsKilled;
					root["rooms"] = $"{Run.Statistics.RoomsExplored} / {Run.Statistics.RoomsTotal}";
					root["scourge"] = Run.Scourge;
					root["distance"] = $"{(Run.Statistics.TilesWalked / 1024f):0.0} {Locale.Get("km")}";

					var id = $"top_{place}";

					GlobalSave.Put(id, Run.Score);
					GlobalSave.Put($"{id}_data", root.ToString());
				}
			}
			
			if (Run.Won) {
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

			Run.SubmitScore?.Invoke(Run.Score, board);
		}
	}
}
