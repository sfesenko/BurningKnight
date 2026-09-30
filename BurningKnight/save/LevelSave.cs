using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BurningKnight.assets.items;
using BurningKnight.entity.item;
using BurningKnight.entity.projectile;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.cutscene;
using BurningKnight.level.hall;
using BurningKnight.level.tile;
using BurningKnight.level.tutorial;
using BurningKnight.level.walls;
using BurningKnight.physics;
using BurningKnight.state;
using Lens.entity;
using Lens.util;
using Lens.util.file;
using Lens.util.math;

namespace BurningKnight.save {
	public class LevelSave : EntitySaver {
		public static int FailedAttempts;
		public static List<int> Fails = new List<int>();
		public static bool XL;
		public static float ChestRewardChance;
		public static float MimicChance;
		public static bool GenerateMarket;
		public static bool GenerateShops;
		public static bool GenerateTreasure;
		public static bool MeleeOnly;
		public static float MobDestructionChance;

		private static int I;

		public override void Save(Area area, FileWriter writer, bool old) {
			SmartSave(area.Tagged[Tags.LevelSave], writer);
				var d = (old ? Run.LastDepth : Run.Depth);
			
			if (d > 0) {
				Run.LastSavedDepth = d;
				Log.Debug($"Set run last saved depth to {Run.LastSavedDepth}");
			}
		}

		public override string GetPath(string path, bool old = false) {
			if (path.EndsWith(".lvl")) {
				return path;
			}
			
			return $"{path}level-{(old ? Run.LastDepth : Run.Depth)}-l{(old ? Run.LastLoop : Run.Loop)}.lvl";
		}

		private RegularLevel CreateLevel() {
			if (Run.Depth < -2) {
				return new CutsceneLevel();
			}
			
			if (Run.Depth == -2) {
				return new TutorialLevel();
			}
			
			if (Run.Depth == 0) {
				return new HallLevel();
			}
			
			return new RegularLevel(BiomeRegistry.GenerateForDepth(Run.Depth));
		}

		public static Biome BiomeGenerated;

		private bool GenerationThread(string seed, Area area, int attempt, int c = 0) {
			// Checked on entry so an abandoned attempt stops before it starts, and again after the
			// slow part so it does not mutate the live area after the watchdog has moved on.
			// The id is captured per attempt: the watchdog moves 'generation' on when it gives up,
			// so a straggler can tell it is no longer current even after the next attempt has
			// started (a bool reset to false at retry would look live again).
			if (attempt != generation) {
				return false;
			}

			var a = new Area();
			Rnd.Seed = $"{seed}{Run.Depth}{c}{Run.Loop}";
			Log.Debug($"Thread seed is {Rnd.Seed} (int {Rnd.IntSeed})");
		
			try {
				Items.GeneratedOnFloor.Clear();
				
				var level = CreateLevel();
				BiomeGenerated = level.Biome;
				WallRegistry.Instance.ResetForBiome(BiomeGenerated);

				a.Add(level);

				if (!level.Generate()) {
					throw new LevelGenerationException("Failed to paint");
				}

				if (attempt != generation) {
					// Tear down our own scratch area, but leave Run.Level alone: the retry may
					// already own it. Destroying our level clears it through its own identity
					// check while it is still ours.
					a.Entities.AddNew();
					a.Destroy();
					return false;
				}

				foreach (var e in a.Entities.ToAdd) {
					area.Add(e);
				}

				area.EventListener.Copy(a.EventListener);
				area.Entities.AddNew();
				I = 0;
			} catch (LevelGenerationException e) {
				if (attempt != generation) {
					// The watchdog gave up while this attempt was failing. Tear down quietly
					// instead of logging an error and retrying a generation nobody is waiting for.
					// Run.Level is left alone for the same reason as above.
					a.Entities.AddNew();
					a.Destroy();
					return false;
				}

				Log.Error(e);
				I++;

				a.Entities.AddNew();
				a.Destroy();
				Run.Level = null;

				if (I > 10) {
					I = 0;
					return GenerationThread(seed, area, attempt, c + 1);
				}
				
				return GenerationThread(seed, area, attempt);
			}

			BiomeGenerated = null;
			return true;
		}

		private string sd;

		// How long level generation may run before it is abandoned and retried with a new seed.
		private const int GenerationTimeout = 7500;

		// How long an abandoned generation thread is given to notice and unwind before the retry
		// starts anyway.
		private const int AbandonGrace = 500;

		// Moved on every time the watchdog gives up on an attempt. Each attempt captures its own
		// value and treats a mismatch as abandoned, so a straggler from an earlier attempt can
		// never mistake itself for the current one.
		private volatile int generation;

		public override void Generate(Area area) {
			// A loop, not recursion: a generator that always overruns would otherwise grow the
			// stack without bound.
			while (true) {
				if (sd == null) {
					sd = Run.Seed;
				}

				var attempt = ++generation;
				var seed = sd;

				var thread = new Thread(() => {
					try {
						GenerationThread(seed, area, attempt);
					} catch (ThreadInterruptedException) {
						// The watchdog below already gave up on this attempt, so this is not a
						// success. Re-establish the physics world, which the interrupted attempt may
						// have left torn down.
						Physics.Destroy();
						Physics.Init();
					}
				});

				Log.Debug("Level gen thread started");

				var stopwatch = Stopwatch.StartNew();
				thread.Start();

				// Wait on the thread rather than polling a flag it writes. Polling a plain bool has
				// no memory ordering, so the read can be hoisted out of the loop and the watchdog
				// never fires, which turns a slow generator into a hang. Join also removes the
				// half-second polling granularity.
				if (!thread.Join(GenerationTimeout)) {
					Log.Debug("Thread took too long, aborting :(");
					// Invalidate the attempt before interrupting, so a straggler that survives
					// the grace wait below still recognises itself as abandoned.
					generation++;
					thread.Interrupt();

					// Thread.Interrupt only takes effect at a wait or sleep, so a thread busy inside
					// level generation may not stop immediately. Wait briefly for it to reach a
					// cancellation check before retrying, otherwise two threads mutate the same
					// physics world and seeded generator at once.
					if (!thread.Join(AbandonGrace)) {
						Log.Error("Level generation thread did not stop after being abandoned;" +
							" retrying while it may still be running");
					}

					Rnd.Seed = Run.Seed = Rnd.GenerateSeed();
					FailedAttempts++;

					sd = null;
					continue;
				}

				stopwatch.Stop();

				if (Run.Depth > 0) {
					Fails.Add(FailedAttempts);
				}

				Log.Debug($"Generation done, took {stopwatch.ElapsedMilliseconds}ms, {FailedAttempts} failed attempts)");
				FailedAttempts = 0;
				sd = null;
				return;
			}
		}

		public override FileHandle GetHandle() {
			return new FileHandle(SaveManager.SlotDir);
		}

		public override void Delete() {
			var handle = Run.Depth > 0 ? GetHandle() : new FileHandle(SaveManager.SaveDir);

			if (!handle.Exists()) {
				return;
			}
			
			foreach (var file in handle.ListFileHandles()) {
				if (file.Extension == ".lvl" || file.Extension == "lvl") {
					file.Delete();
				}
			}
		}

		public LevelSave() : base(SaveType.Level) {
			ResetGen();
		}

		public static void ResetGen() {
			XL = false;
			ChestRewardChance = 5;
			MobDestructionChance = 0;
			MimicChance = 5;
			GenerateMarket = false;
			GenerateTreasure = false;
			GenerateShops = false;
			MeleeOnly = false;
			Item.Attact = false;
		}
	}
}