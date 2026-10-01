using System;
using BurningKnight.entity.component;
using BurningKnight.entity.item.use.parent;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of DoUsesIfUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DoUsesIfUse {
		public new static void RenderDebug(JsonNode root) {
			DoUsesUse.RenderDebug(root);
			ImGui.Separator();

			var option = root["opt"].Int(0);

			if (ImGui.Combo("If", ref option, options, options.Length)) {
				root["opt"] = option;
			}
		}
	}
}
