using BurningKnight.state;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of EnableScourgeUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class EnableScourgeUse {
		public static void RenderDebug(JsonValue root) {
			root.InputText("Scourge", "scourge");
		}
	}
}
