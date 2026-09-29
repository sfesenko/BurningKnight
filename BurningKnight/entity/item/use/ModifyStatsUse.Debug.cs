using System;
using BurningKnight.assets.particle.custom;
using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyStatsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyStatsUse {
		public static void RenderDebug(JsonValue root) {
			root.InputFloat("Speed", "speed", 0);
			root.Checkbox("Add Speed", "add_speed");
			
			root.InputFloat("Damage", "damage", 0);
			root.Checkbox("Add Damage", "add_damage");
			
			root.InputFloat("Fire Rate", "fire_rate", 0);
			root.Checkbox("Add Fire Rate", "add_fire_rate");
			
			root.InputFloat("Ranged Rate", "ranged_rate", 0);
			root.Checkbox("Add Ranged Rate", "add_ranged_rate");
			
			root.InputFloat("Accuracy", "accuracy", 0);
			root.Checkbox("Add Accuracy", "add_accuracy");
			
			root.InputFloat("Range", "range", 0);
			root.Checkbox("Add Range", "add_range");
			
			root.InputFloat("Knockback", "knockback", 0);
			root.Checkbox("Add Knockback", "add_knockback");
		}
	}
}
