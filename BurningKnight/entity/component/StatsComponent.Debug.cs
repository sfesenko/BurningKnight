using System;
using BurningKnight.assets.achievements;
using BurningKnight.entity.events;
using BurningKnight.entity.item.util;
using BurningKnight.level.rooms;
using ImGuiNET;
using Lens.entity;
using Lens.entity.component;
using Lens.util;
using Lens.util.file;

namespace BurningKnight.entity.component {
	// The editor half of StatsComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class StatsComponent {
		public override void RenderDebug() {
			base.RenderDebug();

			ImGui.InputFloat("Speed", ref speed);
			ImGui.InputFloat("Damage", ref damage);
			ImGui.InputFloat("Fire Rate", ref fireRate);
			ImGui.InputFloat("Ranged Rate", ref rangedRate);
			ImGui.InputFloat("Accuracy", ref accuracy);
			ImGui.InputFloat("Range", ref range);
			ImGui.InputFloat("Knockback", ref knockback);
			
			ImGui.Separator();
			
			ImGui.InputFloat("DM Chance", ref DMChance);
			ImGui.InputFloat("Granny Chance", ref GrannyChance);

			ImGui.Checkbox("Saw Deal", ref SawDeal);
			ImGui.Checkbox("Took Deal", ref TookDeal);
			
			ImGui.Separator();
			
			ImGui.Checkbox("Took Damage in Room", ref TookDamageInRoom);
			ImGui.Checkbox("Took Damage on Level", ref TookDamageOnLevel);
			ImGui.InputInt("Containers payed", ref HeartsPayed);
		}
	}
}
