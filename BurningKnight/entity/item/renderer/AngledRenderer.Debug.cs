using System;
using BurningKnight.assets;
using BurningKnight.entity.component;
using BurningKnight.state;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.graphics;
using Lens.input;
using Lens.lightJson;
using Lens.util;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.entity.item.renderer {
	// The editor half of AngledRenderer; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class AngledRenderer {
		public static void RenderDebug(string id, JsonValue parent, JsonValue root) {
			ItemRenderer.RenderDebug(id, parent, root);

			var invert = root["invert_back"].AsBoolean;

			if (ImGui.Checkbox("Invert back?", ref invert)) {
				root["invert_back"] = invert;
			}
			
			var min = (float) root["aa"].Number(0);

			if (ImGui.InputFloat("Added Angle", ref min)) {
				root["aa"] = min;
			}
		}
	}
}
