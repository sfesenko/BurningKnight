using BurningKnight.entity.events;
using BurningKnight.entity.room.controllable.spikes;
using BurningKnight.level;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of PreventDamageUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class PreventDamageUse {
		public static void RenderDebug(JsonNode root) {
			var v = root["lv"].Bool(false);

			if (ImGui.Checkbox("From lava", ref v)) {
				root["lv"] = v;
			}
			
			v = root["sp"].Bool(false);

			if (ImGui.Checkbox("From spikes", ref v)) {
				root["sp"] = v;
			}
			
			v = root["cs"].Bool(false);

			if (ImGui.Checkbox("From chasm", ref v)) {
				root["cs"] = v;
			}
			
			v = root["bms"].Bool(false);

			if (ImGui.Checkbox("From explosions", ref v)) {
				root["bms"] = v;
			}

			root.Checkbox("From Contact", "cnt", false);
		}
	}
}
