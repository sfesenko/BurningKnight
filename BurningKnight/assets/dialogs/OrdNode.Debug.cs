using ImGuiNET;

namespace BurningKnight.assets.dialogs {
	// The editor's half of the node. A release build excludes every *.Debug.cs (ADR-0003).
	public partial class OrdNode {
		public override void RenderElements() {
			ImGui.PushItemWidth(200);
			Inputs[0].Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;
			ImGui.InputText($"Header##{Id}", ref header, 128);
			
			ImGui.Separator();
			ImGui.InputText($"Option A##{Id}", ref optionA, 128);
			Outputs[0].Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;
			ImGui.InputText($"Answer A##{Id}", ref answerA, 128);
			
			ImGui.Separator();
			ImGui.InputText($"Option B##{Id}", ref optionB, 128);
			Outputs[1].Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;
			ImGui.InputText($"Answer B##{Id}", ref answerB, 128);
			ImGui.PopItemWidth();
		}
	}
}
