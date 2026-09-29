using ImGuiNET;
using Lens.entity;
using Lens.entity.component;
using Microsoft.Xna.Framework;

namespace BurningKnight.assets.lighting {
	// The editor half of LightComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class LightComponent {
		public override void RenderDebug() {
			base.RenderDebug();
			var v = Light.Radius;

			if (ImGui.DragFloat("Radius", ref v)) {
				Light.Radius = v;
			}

			var color = new System.Numerics.Vector4(Light.Color.R / 255f, Light.Color.G / 255f, Light.Color.B / 255f, Light.Color.A / 255f);

			if (ImGui.DragFloat4("Color", ref color)) {
				Light.Color = new Color(color.X * 255, color.Y * 255, color.Z * 255, color.W * 255);
			}
		}
	}
}
