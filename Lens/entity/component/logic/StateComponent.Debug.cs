using System;
using ImGuiNET;
using Lens.entity.component.graphics;

namespace Lens.entity.component.logic {
	// The editor half of StateComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class StateComponent {
		public override void RenderDebug() {
			ImGui.Text($"State: {(state == null ? "null" : state.GetType().Name)}");
			var paused = Pause > 0;

			if (ImGui.Checkbox("Paused", ref paused)) {
				Pause = paused ? 1 : 0;
			}
		}
	}
}
