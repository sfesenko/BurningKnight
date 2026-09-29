using System;
using System.Collections.Generic;
using BurningKnight.debug;
using BurningKnight.entity.projectile;
using BurningKnight.state;
using BurningKnight.ui.imgui;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.graphics;
using Lens.graphics.gamerenderer;
using Lens.util.camera;
using Lens.util.tween;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BurningKnight.assets.lighting {
	// The editor half of Lights; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Lights {
		public static void RenderDebug() {
			if (!WindowManager.Lighting) {
				return;
			}
			
			if (!ImGui.Begin("Lighting", ImGuiWindowFlags.AlwaysAutoResize)) {
				ImGui.End();
				return;
			}

			ImGui.Checkbox("Enabled", ref LevelLayerDebug.Lights);
			ImGui.Checkbox("Enable fog", ref EnableFog);
			ImGui.DragFloat("Radius mod", ref RadiusMod);

			ImGui.Separator();
			
			ImGui.InputFloat("Surface alpha", ref alpha);
			ImGui.InputFloat3("Surface tint", ref color);

			if (ImGui.Combo("Surface blend", ref surfaceBlendId, blends, blends.Length)) {
				surfaceBlend = BlendIdToBlend(surfaceBlendId);
			}
			
			ImGui.Separator();
			
			if (ImGui.Combo("Light blend", ref lightBlendId, blends, blends.Length)) {
				lightBlend = BlendIdToBlend(lightBlendId);
			}
			
			var c = new System.Numerics.Vector4(ClearColor.R / 255f, ClearColor.G / 255f, ClearColor.B / 255f, ClearColor.A / 255f);

			if (ImGui.InputFloat4("Color", ref c)) {
				ClearColor = new Color(c.X, c.Y, c.Z, c.W);
			}
			
			ImGui.End();
		}
	}
}
