using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.item.stand;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.use {
	// The editor half of GiveItemUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GiveItemUse {
		public static void RenderDebug(JsonValue root) {
			var stand = root["on_stand"].Bool(false);
			var random = root["random"].Bool(false);

			if (ImGui.Checkbox("Spawn on stand?", ref stand)) {
				root["on_stand"] = stand;
			}
			
			if (ImGui.Checkbox("Random item?", ref random)) {
				root["random"] = random;
			}
			
			if (stand) {
				return;
			}
			
			var val = root["amount"].Int(1);

			if (!random) {
				var item = root["item"].AsString ?? "";

				if (ImGui.InputText("Item", ref item, 128)) {
					root["item"] = item;
				}

				if (!Items.Datas.ContainsKey(item)) {
					ImGui.BulletText("Unknown item!");
				}
			}
			
			if (ImGui.InputInt("Amount", ref val)) {
				root["amount"] = val;
			}

			root.Checkbox("Animate?", "animate", true);
			root.Checkbox("Hide?", "hide", false);
		}
	}
}
