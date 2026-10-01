using BurningKnight.assets.particle.custom;
using BurningKnight.state;
using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of ScourgeUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ScourgeUse {
		public static void RenderDebug(JsonNode root) {
			root.InputInt("Amount", "amount");
		}
	}
}
