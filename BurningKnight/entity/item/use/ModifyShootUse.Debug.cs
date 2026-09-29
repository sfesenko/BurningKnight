using BurningKnight.entity.events;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyShootUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyShootUse {
		public static void RenderDebug(JsonValue root) {
			root.InputInt("Amount", "amount", 1);
		}
	}
}
