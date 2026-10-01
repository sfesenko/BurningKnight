using BurningKnight.entity.events;
using BurningKnight.util;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;

namespace BurningKnight.entity.item.use {
	// The editor half of AffectDealChanceUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class AffectDealChanceUse {
		public static void RenderDebug(JsonNode root) {
			root.Checkbox("Open Both if one is present", "bt", false);
			
			ImGui.Separator();
			
			root.InputFloat("Granny", "gr", 0);
			root.Checkbox("Add Granny", "agr", true);

			ImGui.Separator();
			
			root.InputFloat("Dm", "dm", 0);
			root.Checkbox("Add Dm", "adm", true);
		}
	}
}
