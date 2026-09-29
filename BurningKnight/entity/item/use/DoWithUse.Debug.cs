using System.Collections.Generic;
using BurningKnight.assets.items;
using BurningKnight.entity.item.use.parent;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of DoWithUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DoWithUse {
		public static void RenderDebug(JsonValue root) {
			root.InputFloat("Chance", "chance", 100f);
			
			if (ImGui.TreeNode("With who")) {
				DoWithTagUse.RenderDebug(root);
				ImGui.TreePop();
			}
			
			ImGui.Separator();
			
			if (!root["uses"].IsJsonArray) {
				root["uses"] = new JsonArray();
			}
			
			ItemEditor.DisplayUse(root, root["uses"]);
		}
	}
}
