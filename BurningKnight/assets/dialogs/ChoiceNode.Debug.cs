using ImGuiNET;

namespace BurningKnight.assets.dialogs {
	// The editor's half of the node. A release build excludes every *.Debug.cs (ADR-0003).
	public partial class ChoiceNode {
		public override void RenderElements() {
			if (choices.Count == 0) {
				choices.Add("");
				AddOutput();
			}
			
			Inputs[0].Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;

			ImGui.PushItemWidth(200);
			
			if (ImGui.InputText($"##{name}", ref label, 256, ImGuiInputTextFlags.EnterReturnsTrue)) {
				name = label;
			}

			ImGui.SameLine();
				
			var add = ImGui.Button("+##choice");
			ImGui.PopItemWidth();
			ImGui.Separator();
			
			var i = 0;
			var toRemove = -1;
			
			foreach (var output in Outputs) {
				var s = choices[i];
				output.Offset.Y = ImGui.GetCursorPos().Y + InputHalfHeight;

				ImGui.Bullet();
				ImGui.SameLine();
				ImGui.PushItemWidth(200);

				ImGui.InputText($"##s_{i}", ref s, 512);
				choices[i] = s;

				ImGui.PopItemWidth();

				if (i > 0) {
					ImGui.SameLine();

					if (ImGui.Button($"-##{i}")) {
						toRemove = i;
					}
				}

				i++;
			}

			if (toRemove > -1) {
				choices.RemoveAt(toRemove);
				RemoveConnection(Outputs[toRemove], true);
			}

			if (add) {
				choices.Add("");
				AddOutput();
			}
		}
	}
}
