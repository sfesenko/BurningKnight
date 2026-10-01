using ImGuiNET;
using System.Collections.Generic;
using BurningKnight.entity.component;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;

namespace BurningKnight.entity.item.use.parent {
	// The editor half of DoWithTagUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DoWithTagUse {
		public static void RenderDebug(JsonNode root) {
			var val = root["self"].Bool(false);

			if (ImGui.Checkbox("Self", ref val)) {
				root["self"] = val;
			}
			
			ImGui.Separator();
			
			val = root["same_room"].Bool(false);

			if (ImGui.Checkbox("Same room?", ref val)) {
				root["same_room"] = val;
			}
			
			val = root["all"].Bool(false);

			if (ImGui.Checkbox("All?", ref val)) {
				root["all"] = val;
			}
			
			var tag = root["tag"].Int(0);
			ImGui.Text($"Tags: {tag}");
			
			for (var i = 0; i < BitTag.Total; i++) {
				var t = BitTag.Tags[i];
				var on = (tag & 1 << i) != 0;
				
				if (ImGui.Checkbox(t.Name, ref on)) {
					if (on) {
						tag |= 1 << i;
					} else {
						tag &= ~(1 << i);
					}

					root["tag"] = tag;
				}
			}
		}
	}
}
