using System;
using BurningKnight.entity.creature.player;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of GiveBombUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GiveBombUse {
		public static void RenderDebug(JsonNode root) {
			var val = root["amount"].Int(1);

			if (ImGui.InputInt("Amount", ref val)) {
				root["amount"] = val;
			}
		}
	}
}
