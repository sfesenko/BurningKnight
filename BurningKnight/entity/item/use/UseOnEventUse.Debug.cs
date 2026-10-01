using System;
using BurningKnight.state;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of UseOnEventUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class UseOnEventUse {
		public static void RenderDebug(JsonNode root) {
			var type = root["tp"].String("");
			var use = root["use"].String("");
			
			if (ImGui.InputText("Use", ref use, 128)) {
				root["use"] = use;
			}

			var c = !UseRegistry.Uses.ContainsKey(use);

			if (c) {
				ImGui.BulletText("Unknown use");
			}
			
			if (ImGui.InputText("Event type", ref type, 256)) {
				root["tp"] = type;
			}

			try {
				Type.GetType(type, true, false);
			} catch (Exception) {
				ImGui.BulletText("Unknown type");
			}
			
			if (c) {
				return;
			}
			
			var us = root["us"];

			if (us == null) {
				us = root["us"] = new JsonObject();
			}

			us["id"] = use;
			
			ItemEditor.DisplayUse(root, us);
		}
	}
}
