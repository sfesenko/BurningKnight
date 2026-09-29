using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.physics;
using BurningKnight.state;
using BurningKnight.ui.editor;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.entity.component.graphics;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.physics;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace BurningKnight.level.entities.building {
	// The editor half of Thing; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Thing {
		public override void RenderImDebug() {
			if (ImGui.InputText("File", ref file, 128)) {
				UpdateSprite();
			}
		
			if (ImGui.InputText("Slice", ref sprite, 128)) {
				UpdateSprite();
			}
			
			ImGui.Checkbox("Has shadow", ref hasShadow);
			ImGui.Checkbox("Custom shadow sprite", ref separateShadow);
			ImGui.Separator();
			ImGui.Checkbox("Has body", ref hasBody);
			
			if (hasBody) {
				var x = collider.X;
				var y = collider.Y;
				var w = collider.Width;
				var h = collider.Height;

				if (ImGui.InputInt("X", ref x)) {
					collider.X = x;
				}
				
				if (ImGui.InputInt("Y", ref y)) {
					collider.Y = y;
				}
				
				if (ImGui.InputInt("W", ref w)) {
					collider.Width = w;
				}
				
				if (ImGui.InputInt("H", ref h)) {
					collider.Height = h;
				}
			
				ImGui.Checkbox("Display collider", ref displayCollider);
			}
		}
	}
}
