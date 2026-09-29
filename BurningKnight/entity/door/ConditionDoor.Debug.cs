using BurningKnight.assets.achievements;
using BurningKnight.entity.creature.npc;
using BurningKnight.save;
using ImGuiNET;
using Lens.entity;
using Lens.util.file;

namespace BurningKnight.entity.door {
	// The editor half of ConditionDoor; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ConditionDoor {
		public override void RenderImDebug() {
			base.RenderImDebug();

			ImGui.Checkbox("Lock in demo", ref lockInDemo);
			ImGui.Combo("Condition", ref condition, conditions, conditions.Length);
		}
	}
}
