using System;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.creature.player;
using BurningKnight.level.biome;
using BurningKnight.level.tile;
using BurningKnight.state;
using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of BucketUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class BucketUse {
		public static void RenderDebug(JsonValue root) {
			root.Checkbox("Water Bucket", "wt", false);
			root.Checkbox("Snow Bucket", "snw", false);
		}
	}
}
