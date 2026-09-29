using System;
using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.item.util;
using BurningKnight.entity.projectile;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.entity;
using Lens.lightJson;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyArcUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyArcUse {
		public static void RenderDebug(JsonValue root) {
			root.Checkbox("From Any Source", "any", false);
			
			root.InputFloat("Chance", "chance");
			root.InputFloat("Damage Modifier", "damage");
			root.InputFloat("Scale", "scale");
			root.Checkbox("Make it mine", "mine", false);

			ImGui.Separator();

			if (root.Checkbox("Random Effect", "rne", false)) {
				root.InputFloat("Effect Change Speed", "ecs", 3f);
			} else {
				if (ImGui.TreeNode("Buff")) {
					if (!BuffRegistry.All.ContainsKey(root.InputText("Buff", "buff", "bk:frozen"))) {
						ImGui.BulletText("Unknown buff!");
					}

					if (!root.Checkbox("Infinite", "infinite_buff", false)) {
						root.InputFloat("Buff Duration", "buff_time");
					}

					ImGui.TreePop();
				}
			}
		}
	}
}
