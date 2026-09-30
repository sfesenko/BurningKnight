using BurningKnight.entity.item.use.parent;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;
using Lens.util.timer;

namespace BurningKnight.entity.item.use {
	// The editor half of DoOnTimerUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class DoOnTimerUse {
		public new static void RenderDebug(JsonValue root) {
			root.InputFloat("Time", "time", 1f);
			ImGui.Separator();
			DoUsesUse.RenderDebug(root);
		}
	}
}
