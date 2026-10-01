using BurningKnight.ui.imgui;
using ImGuiNET;
using System;
using System.Collections.Immutable;
using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.entity.component;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.item;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.item.use;
using BurningKnight.save;
using BurningKnight.util;
using Lens;
using Lens.assets;
using Lens.graphics;
using Lens.input;
using Lens.lightJson;
using Lens.util;
using Microsoft.Xna.Framework.Input;
using Num = System.Numerics;

namespace BurningKnight.state {
	public partial class ItemEditor {
		public static void DisplayUse(JsonValue parent, JsonValue root, string? useId = null) {
			if (root == JsonValue.Null) {
				return;
			}

			ImGui.PushID(ud);
			ud++;
			
			if (!root.IsJsonArray && parent != JsonValue.Null) {
				if (ImGui.Button("-")) {
					if (parent.IsJsonArray) {
						toRemove = parent.AsJsonArray.IndexOf(root);
					}	else if (root.IsJsonObject) {
						root.AsJsonObject.Clear();
					}
					
					return;
				}
				
				ImGui.SameLine();
			}

			if (root.IsString) {
				if (ImGui.TreeNode(root.AsString)) {
					ImGui.TreePop();
				}
			} else if (root.IsJsonObject && root["id"] != JsonValue.Null) {
				var rootId = root["id"].AsString;
				
				if (ImGui.TreeNode(rootId)) {
					root.Checkbox("Single Use", "single", false);
					ImGui.Separator();
					
					if (UseRegistry.Renderers.TryGetValue(rootId, out var renderer)) {
						renderer(root);
					} else {
						ImGui.Text($"No renderer found for use '{rootId}'");
					}
					
					ImGui.TreePop();
				}
			} else if (root.IsJsonArray) {
				foreach (var u in root.AsJsonArray) {
					DisplayUse(root, u);
				}

				if (toRemove > -1) {
					root.AsJsonArray.Remove(toRemove);
					toRemove = -1;
				}

				if (useId != null) {
					if (ImGui.Button("Add")) {
						root.AsJsonArray.Add(new JsonObject {
							["id"] = useId
						});
					}
				} else {
					if (ImGui.Button("Add use")) {
						toAdd = root;
						ImGui.OpenPopup("Add item use");
					}
				}

				if (ImGui.BeginPopupModal("Add item use")) {
					id = 0;
					
					ImGui.SetWindowSize(popupSize);
					popupFilter.Draw("");
					ImGui.BeginChild("ScrollingRegionUses", new System.Numerics.Vector2(0, -ImGui.GetStyle().ItemSpacing.Y - ImGui.GetFrameHeightWithSpacing() - 4), 
						false, ImGuiWindowFlags.HorizontalScrollbar);

					foreach (var i in UseRegistry.Uses) {
						ImGui.PushID(id);
				
						if (popupFilter.PassFilter(i.Key) && ImGui.Selectable(i.Key, selectedUse == i.Key)) {
							selectedUse = i.Key;
						}

						ImGui.PopID();
						id++;
					}

					ImGui.EndChild();
					ImGui.Separator();

					if (ImGui.Button("Add") || Input.Keyboard.WasPressed(Keys.Enter, true)) {
						toAdd.AsJsonArray.Add(new JsonObject {
							["id"] = selectedUse
						});

						ImGui.CloseCurrentPopup();
					}

					ImGui.SameLine();
				
					if (ImGui.Button("Cancel") || Input.Keyboard.WasPressed(Keys.Escape, true)) {
						ImGui.CloseCurrentPopup();
					}
				
					ImGui.EndPopup();
				}
			} else {
				if (ImGui.TreeNode(root.ToString())) {
					ImGui.TreePop();
				}
			}
			
			ImGui.PopID();
		}
		private static void DisplayRenderer(JsonValue parent, JsonValue root) {
			var nil = root == JsonValue.Null || root["id"] == JsonValue.Null;
			
			if (nil) {
				ImGui.Text("None");
			} else {
				var id = root["id"].AsString;

				if (RendererRegistry.DebugRenderers.TryGetValue(id, out var renderer)) {
					ImGui.PushID(ud);
					ud++;
					renderer(Selected!.Id, parent, root);
					ImGui.PopID();
				} else {
					ImGui.Text($"No renderer found for '{id}'");
				}
			}

			if (ImGui.Button(nil ? "Add renderer" : "Replace")) {
				toAdd = parent;
				ImGui.OpenPopup("Select renderer");
			}

			if (!nil) {
				ImGui.SameLine();
				
				if (ImGui.Button("Remove")) {
					parent["renderer"] = JsonValue.Null;
					Selected!.Renderer = JsonValue.Null;
				}
			}

			if (ImGui.BeginPopupModal("Select renderer")) {
				ImGui.SetWindowSize(popupSize);
				
				popupFilter.Draw("");
				ImGui.BeginChild("ScrollingRegionRenderers", new System.Numerics.Vector2(0, -ImGui.GetStyle().ItemSpacing.Y - ImGui.GetFrameHeightWithSpacing() - 4), 
					false, ImGuiWindowFlags.HorizontalScrollbar);

				foreach (var i in RendererRegistry.Renderers) {
					ImGui.PushID(id);
				
					if (popupFilter.PassFilter(i.Key) && ImGui.Selectable(i.Key, selectedRenderer == i.Key)) {
						selectedRenderer = i.Key;
					}

					ImGui.PopID();
					id++;
				}

				ImGui.EndChild();
				ImGui.Separator();
				
				if (ImGui.Button("Select") || Input.Keyboard.WasPressed(Keys.Enter, true)) {
					toAdd["renderer"] = new JsonObject {
						["id"] = selectedRenderer
					};

					Selected!.Renderer = toAdd["renderer"];
					
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
