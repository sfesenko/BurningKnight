using System;
using BurningKnight.entity.creature.player;
using ImGuiNET;
using Lens.lightJson;
using Lens.util;

namespace BurningKnight.entity.item {
	// The editor half of Chance; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Chance {
		public void RenderDebug() {
			/*ImGui.Checkbox("Show simplified", ref simplify);
			
			if (simplify) {
				var vl = Math.Pow(Any, -1);
				
				ImGui.Text("1 in");
				ImGui.SameLine();

				if (ImGui.InputDouble("Chance", ref vl)) {
					Any = Math.Pow(vl, -1);
				}
				
				ImGui.Separator();
			
				vl = Math.Pow(Melee, -1);

				ImGui.Text("1 in");
				ImGui.SameLine();
				
				if (ImGui.InputDouble("Melee", ref Melee)) {
					Melee = Math.Pow(vl, -1);
				}
				
				vl = Math.Pow(Magic, -1);

				ImGui.Text("1 in");
				ImGui.SameLine();
				
				if (ImGui.InputDouble("Magic", ref Magic)) {
					Magic = Math.Pow(vl, -1);
				}
				
				vl = Math.Pow(Range, -1);

				ImGui.Text("1 in");
				ImGui.SameLine();
				
				if (ImGui.InputDouble("Range", ref Range)) {
					Range = Math.Pow(vl, -1);
				}

				return;
			}*/
			
			ImGui.InputDouble("Chance", ref Any);
			/*ImGui.Separator();
			
			ImGui.InputDouble("Melee", ref Melee);
			ImGui.InputDouble("Magic", ref Magic);
			ImGui.InputDouble("Range", ref Range);*/
		}
	}
}
