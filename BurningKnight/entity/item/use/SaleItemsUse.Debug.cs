using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.item.stand;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of SaleItemsUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SaleItemsUse {
		public static void RenderDebug(JsonValue root) {
			var v = root["prc"].Number(50f);

			if (ImGui.InputFloat("% sale", ref v)) {
				root["prc"] = v;
			}

			root.Checkbox("Only when used", "wu", false);
		}
	}
}
