using BurningKnight.assets.particle.custom;
using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of BlockDamageUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class BlockDamageUse {
		public static void RenderDebug(JsonNode root) {
			root.InputFloat("Chance", "chance", 10);
		}
	}
}
