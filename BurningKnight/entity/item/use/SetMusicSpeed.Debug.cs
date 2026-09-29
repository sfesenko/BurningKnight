using BurningKnight.util;
using Lens.assets;
using Lens.entity;
using Lens.lightJson;
using Lens.util.tween;

namespace BurningKnight.entity.item.use {
	// The editor half of SetMusicSpeed; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SetMusicSpeed {
		public static void RenderDebug(JsonValue root) {
			root.InputFloat("Speed", "speed", 1f);
		}
	}
}
