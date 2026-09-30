using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util.camera;
using Lens.util.timer;

namespace BurningKnight.entity.item.use {
	// The editor half of ShootQueueUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ShootQueueUse {
		public new static void RenderDebug(JsonValue root) {
			SimpleShootUse.RenderDebug(root);
			
			var amount = root["amn"].Int(3);

			if (ImGui.InputInt("Projectile Count", ref amount)) {
				root["amn"] = amount;
			}
			
			var delay = root["dl"].Number(0.1f);
			
			if (ImGui.InputFloat("Projectile Delay", ref delay)) {
				root["dl"] = delay;
			}
		}
	}
}
