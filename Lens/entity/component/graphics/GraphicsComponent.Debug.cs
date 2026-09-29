using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Lens.entity.component.graphics {
	// The editor half of GraphicsComponent; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class GraphicsComponent {
		public override void RenderDebug() {
			base.RenderDebug();
			ImGui.Checkbox("Visible", ref Enabled);
		}
	}
}
