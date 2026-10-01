using BurningKnight.assets.items;
using BurningKnight.entity.bomb;
using BurningKnight.entity.component;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.entity.item.use {
	// The editor half of SpawnBombUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SpawnBombUse {
		public static void RenderDebug(JsonNode root) {
			var val = root["timer"].Number(2);

			if (ImGui.InputFloat("Timer", ref val)) {
				root["timer"] = val;
			}
			
			var am = root["amount"].Int(1);

			if (ImGui.InputInt("Amount", ref am)) {
				root["amount"] = am;
			}

			var randomly = root["randomly"].Bool(false);

			if (ImGui.Checkbox("Randomly", ref randomly)) {
				root["randomly"] = randomly;
			}
		}
	}
}
