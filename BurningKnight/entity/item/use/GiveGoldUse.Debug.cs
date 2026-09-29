using BurningKnight.entity.creature.player;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of GiveGoldUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GiveGoldUse {
		public static void RenderDebug(JsonValue root) {
			var val = root["amount"].Int(1);

			ImGui.InputInt("Amount", ref val);
			root["amount"] = val;
		}
	}
}
