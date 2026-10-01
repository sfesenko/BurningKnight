using System;
using BurningKnight.assets;
using BurningKnight.entity.component;
using ImGuiNET;
using Lens.graphics;
using Lens.input;
using System.Text.Json.Nodes;
using Lens.util;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.item.renderer {
	// The editor half of StickRenderer; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class StickRenderer {
		public new static void RenderDebug(string id, JsonNode parent, JsonNode root) {			
			ItemRenderer.RenderDebug(id, parent, root);

			var h = root["h"].Bool(false);

			if (ImGui.Checkbox("Horizontal", ref h)) {
				root["h"] = h;
			}
			
			var mv = (float) root["mv"].Number(0);

			if (ImGui.InputFloat("Move", ref mv)) {
				root["mv"] = mv;
			}
			
			var mt = (float) root["mt"].Number(0.1f);

			if (ImGui.InputFloat("Move Time", ref mt)) {
				root["mt"] = mt;
			}
			
			var rt = (float) root["rt"].Number(0.2f);

			if (ImGui.InputFloat("Return Time", ref rt)) {
				root["rt"] = rt;
			}
		}
	}
}
