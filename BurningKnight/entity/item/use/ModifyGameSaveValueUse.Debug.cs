using BurningKnight.save;
using ImGuiNET;
using Lens.entity;
using Lens.lightJson;

namespace BurningKnight.entity.item.use {
	// The editor half of ModifyGameSaveValueUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class ModifyGameSaveValueUse {
		public static void RenderDebug(JsonValue root) {
			var id = root["idd"].String("");

			if (ImGui.InputText("Field id##gs", ref id, 128)) {
				root["idd"] = id;
			}
			
			var amount = root["am"].Number(0);

			if (ImGui.InputFloat("Amount##gs", ref amount)) {
				root["am"] = amount;
			}
			
			var over = root["ov"].Bool(false);

			if (ImGui.Checkbox("Override?", ref over)) {
				root["ov"] = over;
			}
		}
	}
}
