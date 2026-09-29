using BurningKnight.entity.buff;
using BurningKnight.entity.component;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of GiveBuffUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GiveBuffUse {
		public static void RenderDebug(JsonValue root) {
			var time = root["time"].Number(1);
			var buff = root["buff"].AsString ?? "";

			if (ImGui.InputText("Buff", ref buff, 128)) {
				root["buff"] = buff;
			}

			if (!BuffRegistry.All.ContainsKey(buff)) {
				ImGui.BulletText("Unknown buff!");
			}

			var infinite = time < 0;

			if (ImGui.Checkbox("Infinite?", ref infinite)) {
				time = infinite ? -1 : 1;
			}

			if (!infinite) {
				if (ImGui.InputFloat("Duration", ref time)) {
					root["time"] = time;
				}
			}
		}
	}
}
