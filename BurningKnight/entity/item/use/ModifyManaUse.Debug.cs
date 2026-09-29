using BurningKnight.entity.component;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyManaUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyManaUse {
		public static void RenderDebug(JsonValue root) {
			root.InputInt("Amount", "amount");
			root.Checkbox("Set To Min", "to_min", false);
			root.Checkbox("Set To Max", "to_max", false);
		}	
	}
}
