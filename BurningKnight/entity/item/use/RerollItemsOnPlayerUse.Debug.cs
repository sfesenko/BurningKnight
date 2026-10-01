using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using ImGuiNET;
using Lens.entity;
using System.Text.Json.Nodes;
using Lens.util;

namespace BurningKnight.entity.item.use {
	// The editor half of RerollItemsOnPlayerUse; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class RerollItemsOnPlayerUse {
		public static void RenderDebug(JsonNode root) {
			var rerollWeapons = root["weapons"].Bool(false);

			if (ImGui.Checkbox("Reroll weapons?", ref rerollWeapons)) {
				root["weapons"] = rerollWeapons;
			}
			
			var rerollArtifacts = root["artifacts"].Bool(true);
			
			if (ImGui.Checkbox("Reroll artifacts?", ref rerollArtifacts)) {
				root["artifacts"] = rerollArtifacts;
			}
		}
	}
}
