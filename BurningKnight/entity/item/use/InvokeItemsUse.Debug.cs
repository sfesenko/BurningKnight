using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of InvokeItemsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class InvokeItemsUse {
		public static void RenderDebug(JsonValue root) {
			root.Checkbox("Pets", "pets", true);
			root.Checkbox("Orbitals", "orbitals", false);
			root.Checkbox("Any", "any", false);
		}
	}
}
