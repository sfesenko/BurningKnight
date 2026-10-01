using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyShootUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyShootUse {
		public static void RenderDebug(JsonNode root) {
			root.InputInt("Amount", "amount", 1);
		}
	}
}
