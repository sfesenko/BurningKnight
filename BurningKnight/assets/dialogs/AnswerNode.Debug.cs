using BurningKnight.ui.dialog;
using ImGuiNET;

namespace BurningKnight.assets.dialogs {
	// The editor's half of the node. A release build excludes every *.Debug.cs (ADR-0003).
	public partial class AnswerNode {
		public override void RenderElements() {
			base.RenderElements();
			ImGui.Combo("Type", ref type, AnswerDialog.Types, AnswerDialog.Types.Length);
		}
	}
}
