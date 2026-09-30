using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.level;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using Lens.entity;
using Lens.util.file;
using Xunit;

namespace BurningKnight.Tests;

// Save/load round-trips: write each saver's file, read it back into fresh state, and compare
// what comes out with what went in. Everything runs in SaveHost's throwaway data directory, so
// no real save is touched.
public class SaveRoundTripTests : IClassFixture<SaveHost> {
	private readonly SaveHost host;

	public SaveRoundTripTests(SaveHost host) {
		this.host = host;
	}

	[Fact]
	public void GlobalSaveRoundTrips() {
		var area = new GameArea();
		var dir = host.NewDir("global");

		GlobalSave.Values.Clear();
		GlobalSave.Put("save_test_flag", true);
		GlobalSave.Put("save_test_count", 7);
		GlobalSave.Put("save_test_ratio", 0.25f);
		GlobalSave.Emeralds = 3;
		GlobalSave.RunId = 4242;

		SaveManager.Save(area, SaveType.Global, path: dir);

		var expected = new Dictionary<string, string>(GlobalSave.Values);

		GlobalSave.Values.Clear();
		GlobalSave.Emeralds = 0;
		GlobalSave.RunId = 0;

		SaveManager.Load(area, SaveType.Global, dir);

		Assert.Equal(expected, GlobalSave.Values);
		Assert.Equal(3, GlobalSave.Emeralds);
		Assert.Equal(4242u, GlobalSave.RunId);
	}

	[Fact]
	public void GameSaveRoundTrips() {
		var area = new GameArea();
		var dir = host.NewDir("game");

		GameSave.Values.Clear();
		GameSave.Put("save_test_flag", true);
		GameSave.Put("save_test_count", 7);

		Context.Run.Depth = 3;
		Context.Run.RealDepth = 3;
		Context.Run.Loop = 2;
		Context.Run.KillCount = 12;
		Context.Run.Time = 34.5f;
		Context.Run.Seed = "save-test-seed";
		Context.Run.Type = RunType.Regular;
		Context.Run.DailyId = 0;
		Context.Run.CustomSeed = true;
		Context.Run.HasRun = false;

		SaveManager.Save(area, SaveType.Game, path: dir);

		GameSave.Values.Clear();
		Context.Run.KillCount = 0;
		Context.Run.Time = 0;
		Context.Run.Seed = null;
		Context.Run.Type = RunType.Regular;
		Context.Run.Loop = 0;
		Context.Run.CustomSeed = false;
		Context.Run.HasRun = false;

		SaveManager.Load(area, SaveType.Game, dir);

		Assert.Equal(12, Context.Run.KillCount);
		Assert.Equal(34.5f, Context.Run.Time);
		Assert.Equal("save-test-seed", Context.Run.Seed);
		Assert.Equal(2, Context.Run.Loop);
		Assert.True(Context.Run.CustomSeed);
		Assert.Equal(3, Context.Run.LastSavedDepth);
		Assert.True(GameSave.IsTrue("save_test_flag"));
		Assert.Equal(7, GameSave.GetInt("save_test_count"));
	}

	// A generated level survives save -> load -> save unchanged: the same entities with the same
	// bytes. Compare as a multiset — SmartSave sorts by type, and the sort is not stable, so the
	// order inside one type may legitimately differ between the two saves.
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(5)]
	[InlineData(10)]
	[InlineData(13)]
	public void LevelSaveRoundTrips(int depth) {
		var level = Generation.Create($"save-level-{depth}", depth);

		try {
			var dir = host.NewDir($"level-{depth}-a");
			SaveManager.Save(level.Area, SaveType.Level, path: dir);
			var before = ReadEntities(dir + $"level-{depth}-l0.lvl", SaveType.Level);

			var loaded = new GameArea();
			Context.Level = null;
			SaveManager.Load(loaded, SaveType.Level, dir);
			loaded.Entities.AddNew();

			var dir2 = host.NewDir($"level-{depth}-b");
			SaveManager.Save(loaded, SaveType.Level, path: dir2);
			var after = ReadEntities(dir2 + $"level-{depth}-l0.lvl", SaveType.Level);

			var onlyBefore = before.Except(after).ToList();
			var onlyAfter = after.Except(before).ToList();

			Assert.True(before.SequenceEqual(after),
				$"only in the first save ({onlyBefore.Count}):\n{string.Join("\n", onlyBefore.Take(20))}" +
				$"\nonly in the second ({onlyAfter.Count}):\n{string.Join("\n", onlyAfter.Take(20))}");

			loaded.Destroy();
		} finally {
			Generation.Release(level);
			Physics.World.Clear();
		}
	}

	[Fact]
	public void PlayerSaveRoundTrips() {
		var area = new GameArea();

		Context.Run.Depth = 1;
		Context.Run.RealDepth = 1;
		Context.Run.Loop = 0;
		Context.Run.Type = RunType.Regular;
		Context.Run.NumPlayers = 1;

		SaveManager.ForType(SaveType.Player).Generate(area);
		area.Entities.AddNew();

		try {
			var dir = host.NewDir("player-a");
			SaveManager.Save(area, SaveType.Player, path: dir);
			var before = ReadEntities(dir + "player.sv", SaveType.Player);

			var loaded = new GameArea();
			SaveManager.Load(loaded, SaveType.Player, dir);
			loaded.Entities.AddNew();

			var dir2 = host.NewDir("player-b");
			SaveManager.Save(loaded, SaveType.Player, path: dir2);
			var after = ReadEntities(dir2 + "player.sv", SaveType.Player);

			Assert.Equal(before, after);

			loaded.Destroy();
		} finally {
			area.Destroy();
			Physics.World.Clear();
		}
	}

	// Reads a save into (type, body) pairs without touching the game's loaders: the framing is
	// the writer's, and a payload the loader would misread shows up as a byte difference.
	private static List<string> ReadEntities(string path, SaveType type) {
		var reader = new FileReader(path);

		Assert.Equal(SaveManager.MagicNumber, reader.ReadInt32());

		var version = reader.ReadInt16();
		Assert.Equal(SaveManager.Version, version);
		Assert.Equal((byte) type, reader.ReadByte());

		reader.SaveVersion = version;

		var count = reader.ReadInt32();
		var entities = new List<string>();
		var lastType = "";

		for (var i = 0; i < count; i++) {
			lastType = reader.ReadString() ?? lastType;

			// The framing the writer used for this file's version.
			var size = version == 2 ? reader.ReadUint16() : reader.ReadInt32();
			var body = new byte[size];

			for (var j = 0; j < size; j++) {
				body[j] = reader.ReadByte();
			}

			entities.Add($"{lastType}:{Convert.ToHexString(body)}");
		}

		Assert.True(reader.Data.Length == reader.Position,
			$"parsed {count} entities and stopped at {reader.Position} of {reader.Data.Length} bytes");
		entities.Sort(StringComparer.Ordinal);

		return entities;
	}
}
