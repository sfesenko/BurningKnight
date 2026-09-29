using System.Linq;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.mob;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util.math;

namespace BurningKnight.entity.item.use {
	// The editor half of KillMobUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class KillMobUse {
		public static void RenderDebug(JsonValue root) {
			var all = root["all"].Bool(false);

			if (ImGui.Checkbox("All?", ref all)) {
				root["all"] = all;
			}

			if (all || root.Checkbox("Half", "half", false)) {
				return;
			}

			var count = root["count"].Int(1);

			if (ImGui.InputInt("Count", ref count)) {
				root["count"] = count;
			}
		}
	}
}
