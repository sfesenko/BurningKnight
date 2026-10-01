using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.level.entities.chest;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of RegenUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RegenUse {
		public static void RenderDebug(JsonNode root) {
			if (root.Checkbox("On Kills", "kills", true)) {
				root.InputInt("Speed", "speed", 10);
			}

			root.Checkbox("On Chests", "chests", false);
			root.Checkbox("On Purchase", "purchase", false);
		}
	}
}
