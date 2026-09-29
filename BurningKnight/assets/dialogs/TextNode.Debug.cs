using ImGuiNET;

namespace BurningKnight.assets.dialogs {
	// The editor's half of the node. A release build excludes every *.Debug.cs (ADR-0003).
	public partial class TextNode {
		public override void RenderElements() {
			if (Inputs.Count > 0) {
				Inputs[0].Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;
			}

			if (Outputs.Count > 0) {
				Outputs[0].Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;
			}

			ImGui.PushItemWidth(200);
			
			if (ImGui.InputText($"##{name}", ref label, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
				name = label;
			}
			
			ImGui.PopItemWidth();
		}
	}
}
