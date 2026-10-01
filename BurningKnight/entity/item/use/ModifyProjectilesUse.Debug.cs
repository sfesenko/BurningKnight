using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using BurningKnight.entity.projectile;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyProjectilesUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyProjectilesUse {
		public static void RenderDebug(JsonNode root) {
			root.Checkbox("From Any Source", "any", false);
			
			root.InputFloat("Chance", "chance");

			root.Checkbox("Set Scale", "samount");
			root.InputFloat("Scale Modifier", "amount");
			root.Checkbox("Set Damage", "sdamage");
			root.InputFloat("Damage Modifier", "damage");
			root.Checkbox("Set Range", "srange");
			root.InputFloat("Range Modifier", "range");
			
			ImGui.Separator();

			if (root.Checkbox("Random Effect", "rne", false)) {
				root.InputFloat("Effect Change Speed", "ecs", 3f);
			} else {
				root.Checkbox("Make Explosive", "explosive", false);

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
