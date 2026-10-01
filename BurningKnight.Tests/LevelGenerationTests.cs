using System;
using System.Collections.Generic;
using System.Linq;
using BurningKnight.entity.room;
using BurningKnight.level;
using BurningKnight.level.rooms;
using Xunit;

namespace BurningKnight.Tests;

// One class, one process-wide game state: xunit's collection parallelisation is off
// (xunit.runner.json), and every case here runs sequentially.
public class LevelGenerationTests {
	// Seeds per case; CI raises it with BK_TEST_SEEDS (the full 10,000 is a scheduled run).
	private static int Seeds =>
		int.TryParse(Environment.GetEnvironmentVariable("BK_TEST_SEEDS"), out var n) && n > 0 ? n : 25;

	[Fact]
	public void GeneratesOneLevel() {
		GameHost.Boot();

		var level = Generation.Create("smoke");

		Assert.NotEmpty(Rooms(level));

		Generation.Release(level);
	}

	// Every depth a run can reach, including the loop's 13 (the cave) — see Exit.Descend.
	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	[InlineData(6)]
	[InlineData(7)]
	[InlineData(8)]
	[InlineData(9)]
	[InlineData(10)]
	[InlineData(11)]
	[InlineData(12)]
	[InlineData(13)]
	public void EverySeedGeneratesAConnectedLevel(int depth) {
		GameHost.Boot();

		for (var i = 0; i < Seeds; i++) {
			var seed = $"suite{depth}-{i}";
			RegularLevel level;

			try {
				level = Generation.Create(seed, depth);
			} catch (Exception e) {
				throw new Exception($"seed {seed}, depth {depth} failed to generate", e);
			}

			AssertLevel(level, $"seed {seed}, depth {depth}");

			Generation.Release(level);
		}
	}

	private static List<Room> Rooms(RegularLevel level) {
		return level.Area!.Entities.Entities.OfType<Room>().ToList();
	}

	private static void AssertLevel(RegularLevel level, string context) {
		var rooms = Rooms(level);

		Assert.True(rooms.Count > 0, $"{context}: no rooms");

		foreach (var room in rooms) {
			foreach (var (other, door) in room.Parent.Connected) {
				Assert.NotNull(door);
				Assert.True(other.Connected.TryGetValue(room.Parent, out var back) && back == door,
					$"{context}: connection with {other.GetType().Name} is not symmetric");
				// The generator picks the door from `Intersect(room, other).GetPoints()`, and that
				// rect is read with min/max — adjacent rooms do not overlap, so the door sits in
				// the band between them, not inside either.
				var between = room.Parent.Intersect(other);

				Assert.True(door.X >= Math.Min(between.Left, between.Right) && door.X <= Math.Max(between.Left, between.Right)
				            && door.Y >= Math.Min(between.Top, between.Bottom) && door.Y <= Math.Max(between.Top, between.Bottom),
					$"{context}: the door to {other.GetType().Name} at {door.X}:{door.Y} lies outside the rooms");
			}
		}

		// The graph is connected: every placed room is reachable from the first one.
		var seen = new HashSet<RoomDef>();
		var queue = new Queue<RoomDef>();

		queue.Enqueue(rooms[0].Parent);
		seen.Add(rooms[0].Parent);

		while (queue.Count > 0) {
			foreach (var next in queue.Dequeue().Connected.Keys) {
				if (seen.Add(next)) {
					queue.Enqueue(next);
				}
			}
		}

		foreach (var room in rooms) {
			Assert.True(seen.Contains(room.Parent),
				$"{context}: {room.Parent.GetType().Name} is not reachable");
		}
	}
}
