using System;
using BurningKnight.assets.achievements;
using BurningKnight.assets.particle;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.level;
using BurningKnight.save;
using BurningKnight.save.statistics;
using Lens;
using Lens.assets;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.state {
	public class Run {
		public Action<int, string> SubmitScore;
		public readonly int ContentEndDepth = BK.Demo ? 5 : 11;

		private int depth = BK.Version.Dev ? 1 : 0;
		public int NextDepth;
		public int LastDepth;

		private int loop;

		public int Loop {
			get => loop;

			set {
				LastLoop = Loop;
				loop = value;
			}
		}

		public int LastLoop;
		public bool CustomSeed; // -934034507
		public int Id;
		public bool Redo;
		public int NumPlayers;

		public int ActualDepth {
			set => depth = value;
		}
		
		public int SavingDepth;
		public bool StartingNew;
		public int KillCount;
		public float Time;
		public bool StartedNew;
		public bool HasRun;
		
		public string Seed;

		public bool IgnoreSeed;
		public int Luck;
		public int Scourge { get; private set; }
		public int PermanentScourge { get; internal set; }
		public bool IntoMenu;
		public RunStatistics Statistics;
		public string NextSeed;
		public int LastSavedDepth;
		public bool AlternateMusic;
		public RunType Type;
		public int Score;
		public int DailyId;
		public byte ChallengeId;
		public bool Won;

		// The initializers that used to read the statics at class-init time; an instance cannot
		// reference its own fields in a field initializer, so they run here.
		public Run() {
			NextDepth = depth;
			LastDepth = depth;
			LastLoop = Loop;
		}
		
		public int Depth {
			get => depth;
			set => NextDepth = value;
		}

		public int RealDepth {
			set { depth = value; }
		}

		public void Update() {
			if (Redo || StartingNew || depth != NextDepth) {
				LastDepth = depth;
				LastLoop = Loop;
				SavingDepth = depth;
				depth = NextDepth;
				StartedNew = StartingNew;

				if (StartingNew) {
					SaveManager.Delete(SaveType.Player, SaveType.Game, SaveType.Level);
				}

				Redo = false;
				StartingNew = false;

				Engine.Instance.SetState(new LoadState {
					Menu = IntoMenu,
					IntoCutscene = depth < -2
				});

				IntoMenu = false;
			}
		}

		public void StartNew(int depth = 1, RunType type = RunType.Regular) {
			if (Statistics != null) {
				Statistics.Done = true;
				Statistics = null;
			}

			// These track which items stands have already offered this run. They are only ever
			// Remove()d on pickup, so without this an id offered by a stand that was unloaded
			// mid-generation would stay blocked for the rest of the process.
			entity.item.stand.LampStand.AlreadyOnStand.Clear();
			entity.item.stand.GarderobeStand.AlreadyOnStand.Clear();
			entity.item.EmeraldStand.AlreadyOnStand.Clear();

			StartingNew = true;
			HasRun = false;
			NextDepth = depth;
			Type = type;
			Loop = 0;
			CustomSeed = false;
			
			if (NextSeed != null) {
				Seed = NextSeed;
				NextSeed = null;
				IgnoreSeed = false;
				CustomSeed = true;
				Log.Debug("Using preset seed");
			} else if (IgnoreSeed) {
				IgnoreSeed = false;
				Log.Debug("Ignoring seed");
			} else {
				Log.Debug("Generating seed");
				// fixme
				Seed = Rnd.GenerateSeed();
			}

			if (Type != RunType.Challenge) {
				ChallengeId = 0;
			} else {
				Log.Info($"Starting challenge {ChallengeId}");
			}

			if (Type == RunType.Daily) {
				var date = DateTime.UtcNow;
				DailyId = CalculateDailyId();
				Log.Debug($"Today is {date.DayOfYear} day of the year {date.Year}, so the daily id is {DailyId}");

				Seed = Rnd.GenerateSeed(8, DailyId);
			} else {
				DailyId = 0;
			}

			if (depth == 1) {
				GlobalSave.RunId++;
			}

			Rnd.Seed = Seed;
			AlternateMusic = Rnd.Chance(0.5f);
			
			Log.Debug($"This run's seed is {Seed}");
		}

		public int CalculateDailyId() {
			var date = DateTime.UtcNow;
			return (date.Year - 2020) * 365 + (date.DayOfYear) - 81;
		}

		public void ResetStats() {
			KillCount = 0;
			Time = 0;
			HasRun = false;
			Luck = 0;
			Scourge = 0;
			Won = false;
			PermanentScourge = 0;
			LastSavedDepth = 0;
			
			entity.item.Scourge.Clear();
		}

		public string FormatTime() {
			return $"{Math.Floor(Time / 3600f)}h {Math.Floor(Time / 60f % 60f)}m {Math.Floor(Time % 60f)}s";
		}

		public void RemoveScourge() {
			if (Scourge == 0) {
				return;
			}
			
			PermanentScourge = Math.Max(0, PermanentScourge - 1);
			Scourge--;
			
			var player = LocalPlayer.Locate(Context.Area);

			if (player == null) {
				return;
			}
			
			TextParticle.Add(player, Locale.Get("scourge"), 1, true, true);
		}

		public void AddScourge(bool permanent = false) {
			Scourge++;
			Context.Audio.PlaySfx("player_cursed");

			if (Scourge >= 10) {
				Achievements.Unlock("bk:scourge_king");
			}

			if (Scourge > 10) {
				Scourge = 10;
			}
			
			var player = LocalPlayer.Locate(Context.Area);

			if (player == null) {
				return;
			}
			
			TextParticle.Add(player, Locale.Get("scourge"), 1, true);
			var center = player.Center;
			
			for (var i = 0; i < 10; i++) {
				var part = new ParticleEntity(Particles.Scourge());
						
				part.Position = center + Rnd.Vector(-4, 4);
				part.Particle.Scale = Rnd.Float(0.4f, 0.8f);
				Context.Level!.Area!.Add(part);
				part.Depth = 1;
			}
			
			if (permanent) {
				PermanentScourge++;

				if (PermanentScourge > 10) {
					PermanentScourge = 10;
				}
			}
		}

		public void ResetScourge() {
			Scourge = PermanentScourge;
		}

		public void CalculateScore() {
			if (Assets.DataModified || Statistics == null) {
				Score = -696969;
				return;
			}
			
			Score = 1000;

			Score += (Depth - 1) * 5000;
			Score += Statistics.CoinsObtained * 10;
			Score += Statistics.Items.Count * 100;
			Score += (int) Statistics.MobsKilled * 10;
			Score += (int) Statistics.RoomsExplored * 2;
			Score += Statistics.BossesDefeated * 1000;
			Score += Loop * 100000;

			if (Won) {
				Score += 5000;
			}
			
			Score -= (int) Statistics.DamageTaken * 100;
			Score -= (int) Time * 2;
			Score -= Statistics.PitsFallen * 1000;

			var multiplier = 1 + Scourge * 0.1f;
			Score = (int) (Score * multiplier);
		}

		public void Win() {
			if (Won) {
				return;
			}
			
			Won = true;
			Statistics.Won = true;
			Player pl = null;

			foreach (var p in Context.Area!.Tagged[Tags.Player]) {
				p.RemoveComponent<PlayerInputComponent>();
				p.GetComponent<HealthComponent>()!.Unhittable = true;

				pl = (Player) p;
			}
			
			((InGameState) Engine.Instance.State).AnimateDoneScreen(pl);
		}

		public string GetLeaderboardId() {
			switch (Type) {
				case RunType.Daily: {
					return $"daily_{DailyId}";
				}

				case RunType.BossRush: {
					return "boss_rush";
				}

				case RunType.Challenge: {
					return $"challenge_{ChallengeId}";
				}

				default: case RunType.Regular: {
					return "high_score";
				}
			}
		}

		public void GoToTutorial() {
			Depth = -3;
		}
	}
}
