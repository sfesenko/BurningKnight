using BurningKnight.entity.projectile;
using BurningKnight.save;
using BurningKnight.util;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyGenUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyGenUse {
		public static void RenderDebug(JsonNode root) {
			root.Checkbox("XL Level", "xl", false);
			root.Checkbox("Generate Market", "gm", false);
			root.Checkbox("Only Shops", "sp", false);
			root.Checkbox("Only Treasure", "tr", false);
			root.Checkbox("Only Generate Melee", "ml", false);
			root.InputFloat("Chest Reward Chance Modifier", "crc", 0);
			root.InputFloat("Mob Not Shoot Chance Modifier", "md", 0);
			root.InputFloat("Mimic Chance Modifier", "mimic", 0);
		}
	}
}
