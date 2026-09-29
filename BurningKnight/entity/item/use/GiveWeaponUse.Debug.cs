using BurningKnight.assets.items;
using BurningKnight.entity.creature.player;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of GiveWeaponUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GiveWeaponUse {
		public static void RenderDebug(JsonValue root) {
			var item = root["item"].AsString ?? "";

			if (ImGui.InputText("Item", ref item, 128)) {
				root["item"] = item;
			}

			if (!Items.Datas.ContainsKey(item)) {
				ImGui.BulletText("Unknown item!");
			}
		}
	}
}
