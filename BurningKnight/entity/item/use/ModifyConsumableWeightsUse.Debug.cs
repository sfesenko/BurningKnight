using System;
using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyConsumableWeightsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyConsumableWeightsUse {
		public static void RenderDebug(JsonNode root) {
			root.InputInt("Modifier", "amount");
			root.Checkbox("Coins", "coins", true);
			root.Checkbox("Keys", "keys", false);
			root.Checkbox("Bombs", "bombs", false);
		}
	}
}
