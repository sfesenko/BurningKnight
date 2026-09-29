using BurningKnight.entity.component;
using BurningKnight.save;
using BurningKnight.ui.editor;
using ImGuiNET;

namespace BurningKnight.entity.room.controllable {
	// The editor half of RoomControllable; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RoomControllable {
		public override void RenderImDebug() {
			base.RenderImDebug();

			var on = On;

			if (ImGui.Checkbox("On", ref on)) {
				if (on) {
					TurnOn();
				} else {
					TurnOff();
				}
			}
		}
	}
}
