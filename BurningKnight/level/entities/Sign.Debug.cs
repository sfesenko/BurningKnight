using BurningKnight.entity.component;
using BurningKnight.ui.dialog;
using ImGuiNET;
using Lens;
using Lens.util.file;

namespace BurningKnight.level.entities {
	// The editor half of Sign; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class Sign {
		public override void RenderImDebug() {
			var d = GetComponent<CloseDialogComponent>();
			var m = d!.Variants!.Length == 0 ? "" : d.Variants[0];

			if (m == null) {
				m = "";
			}
			
			if (ImGui.InputText("Message", ref m, 128)) {
				SetMessage(m);
			}
			
			if (ImGui.InputText("Sprite", ref Region, 128)) {
				UpdateSprite();
			}

			ImGui.Checkbox("Demo only", ref DemoOnly);
		}
	}
}
