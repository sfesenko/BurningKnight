using System.Collections.Generic;
using BurningKnight.state;
using BurningKnight.ui.imgui;
using BurningKnight.util;
using ImGuiNET;
using Lens.input;
using Lens.lightJson;
using Lens.util.math;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.entity.creature.drop {
	// The editor half of SimpleDrop; a release build excludes every *.Debug.cs (ADR-0003).
	public partial class SimpleDrop {
		private static System.Numerics.Vector2 popupSize = new System.Numerics.Vector2(400, 400);
		private static ImGuiTextFilterPtr? itemFilter;
		private static unsafe ImGuiTextFilterPtr PopupFilter =>
			itemFilter ??= new ImGuiTextFilterPtr(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
		private static string? selectedItem;
		private static int id;
		public static void RenderDebug(JsonValue root) {
			root.InputFloat("Chance", "chance");
			
			root.InputInt("Min", "min");
			root.InputInt("Max", "max");
			
			if (!root["items"].IsJsonArray) {
				root["items"] = new JsonArray();
			}

			var toRemove = -1;
			var items = root["items"].AsJsonArray;

			for (var i = 0; i < items.Count; i++) {
				if (ImGui.SmallButton($"{items[i]}##s")) {
					WindowManager.ItemEditor = true;
					ItemEditor.Selected = assets.items.Items.Datas[items[i]];
				}
				
				ImGui.SameLine();

				if (ImGui.SmallButton("-")) {
					toRemove = i;
				}
			}

			if (toRemove != -1) {
				items.Remove(toRemove);
			}

			if (ImGui.Button("Add")) {
				ImGui.OpenPopup("Add Item##p");	
			}
			
			ImGui.Separator();

			if (ImGui.BeginPopupModal("Add Item##p")) {
				ImGui.SetWindowSize(popupSize);
				
				PopupFilter.Draw("");
				ImGui.BeginChild("ScrollinegionUses##reee", new System.Numerics.Vector2(0, -ImGui.GetStyle().ItemSpacing.Y - ImGui.GetFrameHeightWithSpacing() - 4), 
					false, ImGuiWindowFlags.HorizontalScrollbar);
				
				ImGui.Separator();

				foreach (var i in assets.items.Items.Datas) {
					ImGui.PushID($"{id}__itm");
				
					if (PopupFilter.PassFilter(i.Key) && !items.Contains(i.Key) && ImGui.Selectable($"{i.Key}##dd", selectedItem == i.Key)) {
						selectedItem = i.Key;
					}

					ImGui.PopID();
					id++;
				}

				id = 0;

				ImGui.EndChild();
				ImGui.Separator();

				if (selectedItem != null && (ImGui.Button("Add") || Input.Keyboard.WasPressed(Keys.Enter, true))) {
					items.Add(selectedItem);
					selectedItem = null;
					ImGui.CloseCurrentPopup();
				}

				ImGui.SameLine();
				
				if (ImGui.Button("Cancel") || Input.Keyboard.WasPressed(Keys.Escape, true)) {
					ImGui.CloseCurrentPopup();
				}

				ImGui.EndPopup();
			}
		}
	}
}
