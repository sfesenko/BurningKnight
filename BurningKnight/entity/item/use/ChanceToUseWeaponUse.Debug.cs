using System;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.state;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of ChanceToUseWeaponUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ChanceToUseWeaponUse {
		public static void RenderDebug(JsonValue root) {
			root.Checkbox("Back", "back", false);
			root.Checkbox("Up", "up", false);
			root.Checkbox("Down", "down", false);
			root.InputFloat("Chance", "chance", 10);
		}
	}
}
