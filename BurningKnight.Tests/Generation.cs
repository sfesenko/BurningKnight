using System;
using BurningKnight.assets.items;
using BurningKnight.level;
using BurningKnight.level.biome;
using BurningKnight.level.walls;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using Lens.entity;
using Lens.util.math;

namespace BurningKnight.Tests;

public static class Generation {
	// Mirrors LevelSave.GenerationThread without its thread, watchdog or retry plumbing: the
	// seed, the biome, the wall registry, the area, then Generate().
	public static RegularLevel Create(string seed, int depth = 1, int loop = 0) {
		var area = new Area();

		// `Run.Depth` is the requested depth (it only sets NextDepth, which the game's loop then
		// promotes); the generation reads the private one through RealDepth. A test sets both —
		// and the default differs by configuration, so it must not be left alone.
		Run.Depth = depth;
		Run.RealDepth = depth;
		Run.Loop = loop;
		Rnd.Seed = $"{seed}{depth}0{loop}";

		Items.GeneratedOnFloor.Clear();

		var level = new RegularLevel(BiomeRegistry.GenerateForDepth(depth));
		LevelSave.BiomeGenerated = level.Biome;
		WallRegistry.Instance.ResetForBiome(LevelSave.BiomeGenerated);

		area.Add(level);

		if (!level.Generate()) {
			throw new InvalidOperationException($"Generation returned false for seed {Rnd.Seed}");
		}

		// The queued entities become live here, as GenerationThread does for the game.
		area.Entities.AddNew();

		return level;
	}

	// The game gets a fresh world per level (LoadState calls Physics.Init) and clears it on state
	// teardown (Physics.Destroy), so it never accumulates; this harness generates hundreds of
	// levels into one world, so it has to clear per generation.
	public static void Release(RegularLevel level) {
		level.Area.Destroy();
		Physics.World.Clear();
	}
}
