using System;
using BurningKnight.entity.creature.player;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyActiveChargeUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyActiveChargeUse {
		public static void RenderDebug(JsonNode root) {
			var percent = root["percent"].Bool(false);

			if (ImGui.Checkbox("Percent?", ref percent)) {
				root["percent"] = percent;
			}
			
			var amount = root["amount"].Number(1f);

			if (ImGui.InputFloat(percent ? "Amount (%)" : "Amount (charge)", ref amount)) {
				root["amount"] = amount;
			}
		}
	}
}
