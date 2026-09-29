using BurningKnight.entity.creature.player;
using BurningKnight.util;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyProjectileTextureUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyProjectileTextureUse {
		public static void RenderDebug(JsonValue root) {
			root.InputText("Texture", "texture", "rect");
		}
	}
}
