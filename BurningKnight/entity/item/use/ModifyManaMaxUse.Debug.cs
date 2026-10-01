using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyManaMaxUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyManaMaxUse {
		public static void RenderDebug(JsonNode root) {
			root.InputInt("Amount", "amount");
		}	
	}
}
