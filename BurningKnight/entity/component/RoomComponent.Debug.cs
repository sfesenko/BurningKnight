using BurningKnight.entity.creature.mob;
using BurningKnight.entity.creature.mob.boss;
using BurningKnight.entity.events;
using BurningKnight.entity.room;
using BurningKnight.level.rooms;
using ImGuiNET;
using Lens.entity.component;

namespace BurningKnight.entity.component {
	// The editor half of RoomComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RoomComponent {
		public override void RenderDebug() {
			ImGui.Text(Room == null ? "null" : $"{Room.Type}#{Room.Y}");
		}
	}
}
